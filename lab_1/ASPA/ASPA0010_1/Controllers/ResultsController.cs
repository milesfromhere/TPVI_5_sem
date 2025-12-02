using ASPA0010_1.DTOs;
using ASPA0010_1.Models;
using Authenticate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResultsCollection;

namespace ASPA0010_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResultsController : ControllerBase
    {
        private readonly IResultsCollection _results;
        private readonly IAuthenticateService _authService;

        public ResultsController(IResultsCollection results, IAuthenticateService authService)
        {
            _authService = authService;
            _results = results;
        }

        [HttpGet("SignIn")]
        [AllowAnonymous]
        public async Task<IActionResult> SignIn([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var result = await _authService.SignInAsync(HttpContext, model.Login, model.Password);

            if (!result) return NotFound("Invalid credentials");

            return Ok("Signed in successfully");
        }

        [HttpGet("SignOut")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _authService.SignOutAsync(HttpContext);
            return Ok("Signed out successfully");
        }

        [HttpGet]
        [Authorize(Policy = "READER")]
        public async Task<ActionResult<List<ResultItem>>> GetAllItemAsync()
        {
            try
            {
                var items = await _results.GetAllAsync();

                if (items.Count == 0)
                {
                    return NoContent();
                }

                return Ok(items);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ResultsCollectionException)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{k}")]
        [Authorize(Policy = "READER")]
        public async Task<ActionResult<ResultItem>> GetItemAsync(int k)
        {
            try
            {
                var item = await _results.GetAsync(k);

                if (item == null)
                {
                    return NotFound();
                }

                return Ok(item);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ResultsCollectionException)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        [Authorize(Policy = "WRITER")]
        public async Task<ActionResult<ResultItem>> AddItemAsync([FromBody] CreateItemRequest value)
        {
            try
            {
                var newItem = await _results.AddAsync(value.Value);
                return CreatedAtAction(nameof(GetItemAsync), new { k = newItem.Id }, newItem);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
           catch (ResultsCollectionException)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{k}")]
        [Authorize(Policy = "WRITER")]
        public async Task<ActionResult> DeleteItemAsync(int k)
        {
            try
            {
                var deleteItem = await _results.RemoveAsync(k);
                return Ok(deleteItem);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ResultsCollectionException)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{k}")]
        [Authorize(Policy = "WRITER")]
        public async Task<ActionResult<ResultItem>> UpdateItemAsync(int k, [FromBody] CreateItemRequest value)
        {
            try
            {
                var upd = await _results.UpdateAsync(k, value.Value);
                return Ok(upd);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ResultsCollectionException)
            {
                return StatusCode(500, "Internal server error");
            }
        }
    }
}