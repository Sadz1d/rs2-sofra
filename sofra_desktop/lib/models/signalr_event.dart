/// Sirov dogadjaj sa bilo kojeg huba, koristi ga privremeni ekran koji dokazuje da SignalR radi.
class SignalREvent {
  SignalREvent({required this.hub, required this.method, required this.data})
      : receivedAt = DateTime.now();

  final String hub;
  final String method;
  final dynamic data;
  final DateTime receivedAt;
}
