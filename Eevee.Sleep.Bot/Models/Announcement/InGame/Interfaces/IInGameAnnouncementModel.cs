using Eevee.Sleep.Bot.Enums;

namespace Eevee.Sleep.Bot.Models.Announcement.InGame.Interfaces;

internal interface IInGameAnnouncementResponse<out T> {
    public T ToModel(string url, AnnouncementLanguage language);
}