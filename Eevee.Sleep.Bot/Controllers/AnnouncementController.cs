using Eevee.Sleep.Bot.Controllers.Mongo.Announcement;
using Eevee.Sleep.Bot.Controllers.Mongo.Announcement.OfficialSite;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Models.Announcement.OfficialSite;
using Microsoft.AspNetCore.Mvc;

namespace Eevee.Sleep.Bot.Controllers;

[ApiController]
[Route("/game/announcement/{locale}")]
public class AnnouncementController : ControllerBase {
    [HttpGet("", Name = "GetAnnouncements")]
    public ActionResult<string> Get(string locale) {
        if (!Enum.TryParse<AnnouncementLanguage>(locale, true, out var language)) {
            return BadRequest();
        }

        var announcement = OfficialSiteAnnouncementIndexController.FindAllByLanguage(language);
        return Ok(announcement.Select(x => x.ToApiResponse()));
    }

    [HttpGet("{announcementId}", Name = "GetAnnouncementDetails")]
    public ActionResult<string> Get(
        AnnouncementDetailController<OfficialSiteAnnouncementDetailModel> officialSiteAnnouncementDetailController,
        string locale,
        string announcementId
    ) {
        if (!Enum.TryParse<AnnouncementLanguage>(locale, true, out var language)) {
            return BadRequest();
        }

        var announcement = officialSiteAnnouncementDetailController.FindById(language, announcementId);
        if (announcement is null) {
            return NotFound();
        }

        return Ok(announcement.ToApiResponse());
    }
}