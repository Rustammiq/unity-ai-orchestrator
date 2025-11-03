# LM Studio Configuratie

LM Studio is een lokale LLM server die draait op je computer.

## Snelle Setup

1. Download en installeer [LM Studio](https://lmstudio.ai/)
2. Open LM Studio
3. Download een model (bijv. Llama 3, Mistral, etc.)
4. Start de lokale server in LM Studio
   - Klik op "Local Server" in de interface
   - Standaard draait op: `http://localhost:1234`
5. Laad een model in LM Studio
6. In Unity: `Window > Unity AI Orchestrator > Settings`
   - Schakel "Enable LM Studio" in
   - API URL: `http://localhost:1234/v1/chat/completions`
   - Model Name: Laat leeg (gebruikt het geladen model) of specificeer een model naam
7. Klik op "Save"

## Standaard Configuratie

- **API URL**: `http://localhost:1234/v1/chat/completions`
- **Model**: Laat leeg om het actief geladen model te gebruiken
- **Auth Header**: Meestal niet nodig voor lokale LM Studio

## Aangepaste Configuratie

Als je LM Studio op een andere poort draait of op een andere machine:
- Wijzig de API URL naar je custom endpoint
- Voor remote access: gebruik het IP adres in plaats van localhost

## Voordelen van LM Studio

- ✅ Volledig lokaal (geen API costs)
- ✅ Privacy (data blijft op je computer)
- ✅ Snelle response tijden (afhankelijk van je hardware)
- ✅ Offline werken mogelijk

## Troubleshooting

**"Could not connect to LM Studio"**
- Check of LM Studio daadwerkelijk draait
- Controleer of de server is gestart in LM Studio
- Verify de URL en poort in de Settings

**"Model not found"**
- Zorg dat je een model hebt geladen in LM Studio
- Check de model naam als je die hebt gespecificeerd
