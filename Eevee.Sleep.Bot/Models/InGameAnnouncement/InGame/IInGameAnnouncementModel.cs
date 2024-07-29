using Eevee.Sleep.Bot.Enums;

namespace Eevee.Sleep.Bot.Models.InGameAnnouncement.InGame;

interface IInGameAnnouncementResponse<T> {
    public T ToModel(string url, InGameAnnoucementLanguage language);
}