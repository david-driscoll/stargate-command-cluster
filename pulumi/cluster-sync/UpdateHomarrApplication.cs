namespace applications;

public record UpdateHomarrApplication(
  string Name,
  string Description,
  string? IconUrl,
  string Href,
  string? PingUrl
);