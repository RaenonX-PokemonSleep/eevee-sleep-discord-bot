using Eevee.Sleep.Bot.Controllers.Mongo.Announcement;
using Eevee.Sleep.Bot.Controllers.Mongo.Announcement.OfficialSite;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Models.Announcement.OfficialSite;
using Microsoft.AspNetCore.Mvc;

namespace Eevee.Sleep.Bot.Controllers;

[ApiController]
[Route("/game/announcement")]
public class InGameAnnouncementController() : ControllerBase {
    [HttpGet("{Locale}", Name = "GetInGameAnnouncements")]
    public ActionResult<string> Get(string Locale) {
        if (!Enum.TryParse<AnnouncementLanguage>(Locale, true, out var language)) {
            return BadRequest();
        }

        var announcement = OfficialSiteAnnouncememntIndexController.FindAllByLanguage(language);
        return Ok(announcement.Select(x => x.ToApiResponse()));
    }

    [HttpGet("{Locale}/{AnnouncementId}", Name = "GetInGameAnnouncementDetails")]
    public ActionResult<string> Get(
        AnnouncementDetailController<OfficialSiteAnnouncementDetailModel> OfficialSiteAnnouncementDetailController,
        string Locale, 
        string AnnouncementId
    ) {
        if (!Enum.TryParse<AnnouncementLanguage>(Locale, true, out var language)) {
            return BadRequest();
        }

        var announcement = OfficialSiteAnnouncementDetailController.FindById(language, AnnouncementId);
        if (announcement is null) {
            return NotFound();
        }

        return Ok(announcement.ToApiResponse());
    }
}