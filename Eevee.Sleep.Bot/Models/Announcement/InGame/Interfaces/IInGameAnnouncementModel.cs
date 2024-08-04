using Eevee.Sleep.Bot.Enums;

namespace Eevee.Sleep.Bot.Models.Announcement.InGame.Interfaces;

interface IInGameAnnouncementResponse<T> {
    public T ToModel(string url, AnnouncementLanguage language);
}