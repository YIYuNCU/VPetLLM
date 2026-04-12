using VPetLLM.Utils.Common;

namespace VPetLLM.Core.Abstractions.Interfaces.Plugin
{
    public interface IXmlTagProvider : IVPetLLMPlugin
    {
        IEnumerable<PluginTagRegistration> GetXmlTagRegistrations();
    }
}
