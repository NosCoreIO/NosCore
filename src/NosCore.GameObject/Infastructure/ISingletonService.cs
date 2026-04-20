//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Infastructure;

// Marker interface: classes that implement this are registered as Singleton by the
// name-convention scan in WolverineDependencyRegistrar. Everything else picked up
// by the *Service / *Queue / *Catalog / *Ai naming convention stays Transient.
//
// Use sparingly — only for things that genuinely own shared state (channel
// dictionaries, lookup caches, per-entity state maps). Stateless helpers stay
// transient so a stray mutation doesn't leak across requests.
public interface ISingletonService { }
