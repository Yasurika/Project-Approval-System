using Microsoft.AspNetCore.Mvc;
using ProjectApprovalSystem.Core.Interfaces;

namespace ProjectApprovalSystem.Web.Controllers;

/// <summary>
/// Controller for proposal-related operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProposalsController : ControllerBase
{
    private readonly IProposalService _proposalService;
    private readonly IBlindMatchService _blindMatchService;

    public ProposalsController(IProposalService proposalService, IBlindMatchService blindMatchService)
    {
        _proposalService = proposalService;
        _blindMatchService = blindMatchService;
    }

    /// <summary>
    /// Submit a new proposal (Student only)
    /// </summary>
    [HttpPost("submit")]
    public async Task<IActionResult> SubmitProposal([FromBody] SubmitProposalRequest request)
    {
        try
        {
            var proposal = await _proposalService.SubmitProposalAsync(
                request.StudentId,
                request.Title,
                request.Abstract,
                request.Description,
                request.TechStack,
                request.ResearchAreaId
            );

            return Created($"api/proposals/{proposal.Id}", new { proposal.Id, proposal.ProposalId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get student's own proposals
    /// </summary>
    [HttpGet("student/{studentId}")]
    public async Task<IActionResult> GetStudentProposals(int studentId)
    {
        var proposals = await _proposalService.GetStudentProposalsAsync(studentId);
        return Ok(proposals);
    }

    /// <summary>
    /// Get anonymous proposals for supervisor (blind matching)
    /// </summary>
    [HttpGet("anonymous/supervisor/{supervisorId}")]
    public async Task<IActionResult> GetAnonymousProposalsForSupervisor(int supervisorId)
    {
        try
        {
            var proposals = await _blindMatchService.GetAnonymousProposalsForSupervisorAsync(supervisorId);
            return Ok(proposals);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get single anonymous proposal (blind matching)
    /// </summary>
    [HttpGet("anonymous/{proposalId}/supervisor/{supervisorId}")]
    public async Task<IActionResult> GetAnonymousProposal(int proposalId, int supervisorId)
    {
        try
        {
            var proposal = await _blindMatchService.GetAnonymousProposalForSupervisorAsync(proposalId, supervisorId);
            if (proposal == null)
                return NotFound();

            return Ok(proposal);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Withdraw a proposal
    /// </summary>
    [HttpPut("{proposalId}/withdraw")]
    public async Task<IActionResult> WithdrawProposal(int proposalId)
    {
        try
        {
            await _proposalService.WithdrawProposalAsync(proposalId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

/// <summary>
/// Controller for matching/interest operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MatchesController : ControllerBase
{
    private readonly IBlindMatchService _blindMatchService;

    public MatchesController(IBlindMatchService blindMatchService)
    {
        _blindMatchService = blindMatchService;
    }

    /// <summary>
    /// Express interest in a proposal (blind matching - no student identity revealed)
    /// </summary>
    [HttpPost("express-interest")]
    public async Task<IActionResult> ExpressInterest([FromBody] ExpressInterestRequest request)
    {
        try
        {
            var match = await _blindMatchService.ExpressInterestAsync(
                request.ProposalId,
                request.SupervisorId,
                request.Comments
            );

            return Created($"api/matches/{match.Id}", new { match.Id });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Confirm match (student identity is now revealed)
    /// </summary>
    [HttpPut("{matchId}/confirm")]
    public async Task<IActionResult> ConfirmMatch(int matchId)
    {
        try
        {
            var match = await _blindMatchService.ConfirmMatchAsync(matchId);
            return Ok(new { message = "Match confirmed", match });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Decline interest in proposal
    /// </summary>
    [HttpPut("{matchId}/decline")]
    public async Task<IActionResult> DeclineMatch(int matchId)
    {
        try
        {
            await _blindMatchService.DeclineMatchAsync(matchId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

// DTOs
public class SubmitProposalRequest
{
    public int StudentId { get; set; }
    public required string Title { get; set; }
    public required string Abstract { get; set; }
    public required string Description { get; set; }
    public required string TechStack { get; set; }
    public int ResearchAreaId { get; set; }
}

public class ExpressInterestRequest
{
    public int ProposalId { get; set; }
    public int SupervisorId { get; set; }
    public string? Comments { get; set; }
}
