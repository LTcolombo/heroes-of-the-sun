using System.Threading.Tasks;

namespace View.Exploration.SmartObjectTypes
{
    public interface ISmartObject
    {
        Task SetEntity(string value);
    }
}