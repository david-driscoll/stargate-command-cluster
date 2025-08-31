namespace applications;

public record HomarrApplication(
  string Id,
  string Name,
  string Description,
  string? IconUrl,
  string Href,
  string? PingUrl
);