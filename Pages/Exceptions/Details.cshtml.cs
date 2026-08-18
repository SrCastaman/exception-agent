using ExceptionAgent.Aplication.Exceptions;
using ExceptionAgent.Contracts;
using ExceptionAgent.Infraestructure.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExceptionAgent.Pages.Exceptions;

public class DetailsModel : PageModel
{
    private readonly ExceptionInvestigationService _investigationService;

    public ExceptionInvestigation? Investigation { get; set; }

    public InvestigationContext? Context { get; set; }

    private readonly AgentService _agentService;

    public DetailsModel(
    ExceptionInvestigationService investigationService,
    AgentService agentService)
    {
        _investigationService = investigationService;
        _agentService = agentService;
    }

    public AgentResult? AgentResult { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Investigation = await _investigationService.InvestigateAsync(id);

        if (Investigation == null)
        {
            return NotFound();
        }

        Context = _investigationService.BuildContext(Investigation);

        AgentResult = await _agentService.AnalyzeAsync(Context);

        return Page();
    }
}