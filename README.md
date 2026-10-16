# CyberBot - Cybersecurity Awareness Chatbot (Part 2: WPF GUI)

A Windows desktop chatbot (WPF, .NET 8) that teaches South African users about phishing, passwords,
malware, firewalls, VPNs, two-factor authentication, scams, privacy and safe browsing.


**Video presentation:** https://advtechonline-my.sharepoint.com/:v:/r/personal/st10500376_rcconnect_edu_za/_layouts/15/stream.aspx?id=%2Fpersonal%2Fst10500376%5Frcconnect%5Fedu%5Fza%2FDocuments%2FLabVM2334547%20%2D%20labvmdnsm4okohpypfuo4%2Esouthafricanorth%2Ecloudapp%2Eazure%2Ecom%5F3389%20%2D%20Remote%20Desktop%20Connection%202026%2D10%2D16%2012%2D09%2D09%2Emp4&share=cQocuQjbOTc0RoV9MlGlvVuiEgUCeY8NlXFGMsob1vur2RBO5g

## How to run
1. Install the .NET 8 SDK (Windows).
2. Place `greeting.wav` in the project folder (next to `CyberBot.csproj`). It is copied to the output folder automatically.
3. From the project folder run: `dotnet run`  (or open `CyberBot.csproj` in Visual Studio 2022 and press F5).

## Features (Part 2)
| Requirement | Where it lives |
|---|---|
| GUI with Part 1 features (voice, ASCII art, name, welcome, validation) | `MainWindow.xaml`, `MainWindow.xaml.cs`, `Utils/AsciiArt.cs`, `Utils/AudioPlayer.cs` |
| Keyword recognition (9 topics, many keywords) | `Services/KnowledgeBase.cs`, `Utils/TextMatcher.cs` |
| Random responses (non-repeating tips) | `Chatbot.NextTip`, `Topic.Tips` |
| Conversation flow ("another tip", "tell me more", "I don't understand") | `Chatbot.HandleFollowUp` |
| Memory & recall (name, favourite topic, topics discussed) | `Models/User.cs`, `Chatbot.HandleInterestStatement`, `Chatbot.Personalise` |
| Sentiment detection (worried, frustrated, overwhelmed, curious, positive) | `Services/SentimentAnalyser.cs` |
| Error handling / default replies | `Chatbot.GetReply`, `Chatbot.HandleDefault` |
| Generic collections & delegates | `List`/`Dictionary`/`HashSet`; `Func<string,bool>` intents in `Chatbot` |

## Example conversation
- `What is phishing?` - definition
- `Give me a phishing tip` - random tip, then `another tip` gives a different one
- `I'm interested in privacy` - bot remembers; later replies are personalised
- `I'm worried about online scams` - empathy **and** a tip straight away
- `What do you remember about me?` - recalls name, favourite topic, topics discussed

## Project structure
```
CyberBot/
  App.xaml / App.xaml.cs          WPF entry point + colour theme and styles
  MainWindow.xaml / .xaml.cs      Chat UI (bubbles, typing effect, side panel)
  Models/                         User, Topic, Sentiment
  Services/                       Chatbot (logic), KnowledgeBase (content), SentimentAnalyser
  Utils/                          AudioPlayer, AsciiArt, TextMatcher
  .github/workflows/dotnet.yml    CI build on every push
```

## Releases (Part 2 requires at least 2 tags)
```
git tag -a v2.0.0 -m "Part 2: WPF GUI, keyword recognition, memory, sentiment"
git push origin v2.0.0
```
Then create a GitHub Release from each tag with release notes.
