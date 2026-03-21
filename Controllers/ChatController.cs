using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projekt_zespołowy.Models;

[Authorize]
public class ChatController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;

    public ChatController(AppDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> RideChat(Guid rideId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var chat = await _context.Chats
            .Include(c => c.Messages)
                .ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c => c.RideId == rideId);

        if (chat == null)
        {
            chat = new Chat
            {
                Id = Guid.NewGuid(),
                RideId = rideId
            };

            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();
        }

        bool isParticipant = await _context.ChatParticipants
            .AnyAsync(p => p.ChatId == chat.Id && p.UserId == user.Id);

        if (!isParticipant)
        {
            TempData["ErrorMessage"] = "Czat nie jest jeszcze dostępny dla tego przejazdu.";
            return RedirectToAction("Details", "Rides", new { id = rideId });
        }

        // 🔒 sprawdzenie czy user jest uczestnikiem
        return View(chat);
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage(Guid chatId, string content)
    {
        var user = await _userManager.GetUserAsync(User);

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            SenderId = user.Id,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();

        var chat = await _context.Chats.FindAsync(chatId);

        return RedirectToAction("RideChat", new { rideId = chat.RideId });
    }
    public async Task<IActionResult> ChatList()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound("Nie jesteś zalogowany.");

        // Pobieramy czaty, w których użytkownik jest kierowcą lub pasażerem
        var chats = await _context.Chats
            .Include(c => c.Ride)
                .ThenInclude(r => r.StartLocation)
            .Include(c => c.Ride)
                .ThenInclude(r => r.EndLocation)
            .Include(c => c.Ride)
                .ThenInclude(r => r.Driver)
                    .ThenInclude(d => d.User)
            .Include(c => c.Messages)
            .Where(c =>
                c.Ride.Driver.UserId == user.Id ||
                c.Ride.Bookings.Any(b => b.PassengerUserId == user.Id && b.Status != BookingStatus.Cancelled))
            .OrderByDescending(c => c.Messages.Max(m => m.CreatedAt))
            .ToListAsync();

        return View(chats);
    }
}