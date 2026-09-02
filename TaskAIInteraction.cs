using GooseShared;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

public class TaskAIInteraction : GooseTaskInfo
{
    private GooseAIConfig _cfg = GooseAIConfig.Load();
    private static List<object> messageHistory;
    private GooseEntity _currentGoose;

    public TaskAIInteraction()
    {
        taskID = "AIInteraction";
        shortName = "AI Interaction";
        description = "The goose interacts with the user using AI.";
        canBePickedRandomly = false;

        _cfg = GooseAIConfig.Load();
        
        if (messageHistory == null)
        {
            messageHistory = new List<object>
            {
                new { role = "system", content = _cfg.systemPrompt }
            };
        }
    }

    public override GooseTaskData GetNewTaskData(GooseEntity goose)
    {
        return new TaskAIInteractionData();
    }
    
    public override void RunTask(GooseEntity goose)
    {
        _currentGoose = goose;
        ShowInputPrompt(goose);
        StartIdleChatter(goose);
    }

    private void ShowInputPrompt(GooseEntity goose)
    {
        var pt = new Point((int)goose.position.x, (int)goose.position.y);
        
        // Create form with callback
        var form = new GooseInputForm(pt, _cfg.bubbleText, goose, HandleInputSubmitted);
        
        // Set form position tracking
        ModMain.SetInputForm(form);
        
        // Show form
        form.Show();
        form.BringToFront();
        
        // Clear reference when form closes
        form.FormClosed += (s, e) => ModMain.ClearInputForm();
    }
    
    private async void HandleInputSubmitted(string userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput)) return;
        
        try
        {
            string aiResponse = await GetAIResponse(userInput);
            string wrapped = WrapText(aiResponse, 50);
            
            var control = new Control();
            control.CreateControl();
            control.Invoke((MethodInvoker)(() => SpeechBubble.Speak(wrapped)));
        }
        catch (Exception ex)
        {
            var control = new Control();
            control.CreateControl();
            string errorMsg = (ex.InnerException != null) ? ex.InnerException.Message : ex.Message;
            control.Invoke((MethodInvoker)(() =>
                SpeechBubble.Speak("AI Error: " + errorMsg)
            ));
        }
    }

    private void StartIdleChatter(GooseEntity goose)
    {
        Task.Run(async () =>
        {
            var lastPos = goose.position;
            int idleTimeMs = 8000;
            int checkInterval = 500;
            int elapsed = 0;
            Random rand = new Random();

            while (true)
            {
                await Task.Delay(checkInterval);
                elapsed += checkInterval;

                if (goose.position.x != lastPos.x || goose.position.y != lastPos.y)
                {
                    elapsed = 0;
                    lastPos = goose.position;
                }

                if (elapsed >= idleTimeMs)
                {
                    if (rand.NextDouble() <= _cfg.chanceToSpeak)
                    {
                        string aiPrompt = "Quack!";
                        string aiResponse = await GetAIResponse(aiPrompt);
                        string wrapped = WrapText(aiResponse, 50);

                        var control = new Control();
                        control.CreateControl();
                        control.Invoke((MethodInvoker)(() => SpeechBubble.Speak(wrapped)));

                        elapsed = 0;
                    }
                }
            }
        });
    }

    private string WrapText(string text, int maxLineLength)
    {
        var words = text.Split(' ');
        var sb = new StringBuilder();
        int currentLineLength = 0;

        foreach (var word in words)
        {
            if (currentLineLength + word.Length + 1 > maxLineLength)
            {
                sb.AppendLine();
                currentLineLength = 0;
            }
            else if (currentLineLength > 0)
            {
                sb.Append(' ');
                currentLineLength++;
            }

            sb.Append(word);
            currentLineLength += word.Length;
        }

        return sb.ToString();
    }

    private async Task<string> GetAIResponse(string userInput)
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        messageHistory.Add(new { role = "user", content = userInput });

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _cfg.api_key);

            var payload = new
            {
                model = _cfg.model,
                messages = messageHistory
            };

            string jsonRequest = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(_cfg.endpoint, content);
            string json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception(string.Format("HTTP {0}: {1}", (int)response.StatusCode, json));

            dynamic result = JsonConvert.DeserializeObject(json);
            string aiReply = result.choices[0].message.content;

            messageHistory.Add(new { role = "assistant", content = aiReply });

            return aiReply;
        }
    }
}

public class TaskAIInteractionData : GooseTaskData
{
    // No additional data needed
}
