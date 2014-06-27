namespace Smarthouse
{
    class Smarthouse
    {
        private ModuleManager moduleManager;
        public Smarthouse(string pathToCfgDir)
        {
            this.moduleManager=new ModuleManager( "pathtocfg" );

        }

    }
}