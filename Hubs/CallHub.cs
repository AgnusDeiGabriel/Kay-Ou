using Microsoft.AspNetCore.SignalR;

namespace KayAyiti.Hubs;

public class CallHub : Hub
{
    public async Task JoinUserGroup(string userId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, "u_" + userId);

    public async Task CallUser(string calleeUserId, string callerName, string callerAvatar) =>
        await Clients.Group("u_" + calleeUserId)
            .SendAsync("IncomingCall", Context.ConnectionId, callerName, callerAvatar);

    public async Task AcceptCall(string callerConnId) =>
        await Clients.Client(callerConnId).SendAsync("CallAccepted", Context.ConnectionId);

    public async Task DeclineCall(string callerConnId) =>
        await Clients.Client(callerConnId).SendAsync("CallDeclined");

    public async Task EndCall(string otherConnId) =>
        await Clients.Client(otherConnId).SendAsync("CallEnded");

    // Caller anile ANVAN callee a aksepte — voye pa group (pa connection ID)
    public async Task CancelCall(string calleeUserId) =>
        await Clients.Group("u_" + calleeUserId).SendAsync("CallCancelled");

    public async Task SendOffer(string targetConnId, string sdp) =>
        await Clients.Client(targetConnId).SendAsync("ReceiveOffer", Context.ConnectionId, sdp);

    public async Task SendAnswer(string targetConnId, string sdp) =>
        await Clients.Client(targetConnId).SendAsync("ReceiveAnswer", sdp);

    public async Task SendIce(string targetConnId, string candidate) =>
        await Clients.Client(targetConnId).SendAsync("ReceiveIce", candidate);
}
