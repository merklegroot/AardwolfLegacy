using StructureMap;

namespace mew_agent_con
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = Container.For<MewAgentRegistry>();
            var app = container.GetInstance<IMewApp>();
            app.Run();
        }
    }
}
