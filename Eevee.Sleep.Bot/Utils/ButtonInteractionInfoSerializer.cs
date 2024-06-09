using Eevee.Sleep.Bot.Models;
using System.Text.Json;

public static class ButtonInteractionInfoSerializer {
    public static string Serialize(ButtonInteractionInfo info) {
        return JsonSerializer.Serialize(info);
    }

    public static ButtonInteractionInfo? Deserialize(string json) {
        return JsonSerializer.Deserialize<ButtonInteractionInfo>(json);
    }
}