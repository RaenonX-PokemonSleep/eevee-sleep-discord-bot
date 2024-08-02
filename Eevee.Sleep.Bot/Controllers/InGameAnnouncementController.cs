using Eevee.Sleep.Bot.Controllers.Mongo.InGameAnnouncement;
using Eevee.Sleep.Bot.Controllers.Mongo.InGameAnnouncement.Officialsite;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Models.InGameAnnouncement.Officialsite;
using Microsoft.AspNetCore.Mvc;

namespace Eevee.Sleep.Bot.Controllers;

[ApiController]
[Route("/game/announcement")]
public class InGameAnnouncementController() : ControllerBase {
    [HttpGet("{Locale}", Name = "GetInGameAnnouncements")]
    public ActionResult<string> Get(string Locale) {
        if (!Enum.TryParse<InGameAnnoucementLanguage>(Locale, true, out var language)) {
            return BadRequest();
        }

        var announcement = OfficialsiteAnnouncememntIndexController.FindAllByLanguage(language);
        return Ok(announcement.Select(x => x.ToApiResponse()));
    }

    [HttpGet("{Locale}/{AnnouncementId}", Name = "GetInGameAnnouncementDetails")]
    public ActionResult<string> Get(
        AnnouncementDetailController<OfficialsiteAnnouncementDetailModel> OfficialsiteAnnouncementDetailController,
        string Locale, 
        string AnnouncementId
    ) {
        if (!Enum.TryParse<InGameAnnoucementLanguage>(Locale, true, out var language)) {
            return BadRequest();
        }

        var announcement = OfficialsiteAnnouncementDetailController.FindById(language, AnnouncementId);
        if (announcement is null) {
            return NotFound();
        }

        return Ok(announcement.ToApiResponse());
    }
}