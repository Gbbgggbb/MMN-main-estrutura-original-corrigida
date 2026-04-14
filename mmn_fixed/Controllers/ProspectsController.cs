using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MMN.Web.Models;

namespace MMN.Web.Controllers;

[Authorize]
public class ProspectsController : Controller
{
    [HttpGet]
    public IActionResult Index(Guid? openChecklist = null)
    {
        ViewData["Title"] = "Painel de Prospectos";
        ViewBag.OpenChecklistId = openChecklist?.ToString();
        return View(new DashboardViewModel());
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"] = "Novo Prospecto";
        return View("Form", new ProspectFormViewModel());
    }

    [HttpGet]
    public IActionResult Edit(Guid id)
    {
        ViewData["Title"] = "Editar Prospecto";
        ViewBag.EditProspectId = id.ToString();
        return View("Form", new ProspectFormViewModel());
    }
}
