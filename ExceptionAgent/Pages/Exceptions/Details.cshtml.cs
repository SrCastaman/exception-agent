using ExceptionAgent.Application.Exceptions;
using ExceptionAgent.Contracts;
using ExceptionAgent.Infraestructure.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExceptionAgent.Pages.Exceptions;

public class DetailsModel : PageModel
{
    private readonly ExceptionInvestigationService _investigationService;
    private readonly AgentService _agentService;

    public ExceptionInvestigation? Investigation { get; set; }

    public InvestigationContext? Context { get; set; }

    public AgentResult? AgentResult { get; set; }

    public DetailsModel(
        ExceptionInvestigationService investigationService,
        AgentService agentService)
    {
        _investigationService = investigationService;
        _agentService = agentService;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Investigation = await _investigationService.InvestigateAsync(id);

        if (Investigation == null)
        {
            return NotFound();
        }

        Context = await _investigationService
            .BuildContextAsync(Investigation);

        AgentResult = await _agentService.AnalyzeAsync(
            Context,
            HttpContext.RequestAborted);

        return Page();
    }
}