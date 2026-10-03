namespace LocalPrintService.Domain.Configuration;

/// <summary>
/// Conexion saliente de esta PC al backend (Tiendas Dominicanas) para
/// recibir trabajos de impresion desde tablets/celulares (Fase 2).
/// Se configura en appsettings.json (seccion "CloudRelay") o con variables
/// de entorno (CloudRelay__Enabled, CloudRelay__BackendUrl, CloudRelay__Token,
/// CloudRelay__AgentName).
/// </summary>
public sealed class CloudRelaySettings
{
    public const string SectionName = "CloudRelay";

    public bool Enabled { get; set; }

    /// <summary>URL base del backend, ej: https://api.tiendasdominicanas.com.do</summary>
    public string BackendUrl { get; set; } = string.Empty;

    /// <summary>Token generado desde el POS (Impresion en la nube > Generar token).</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Nombre de esta PC de caja (opcional; usa el nombre de la maquina si esta vacio).</summary>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>Espera entre intentos de reconexion al backend.</summary>
    public int ReconnectDelaySeconds { get; set; } = 5;
}
