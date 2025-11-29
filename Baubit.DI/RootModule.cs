//using Baubit.Configuration;
//using Microsoft.Extensions.Configuration;
//using System;
//using System.Collections.Generic;

//namespace Baubit.DI
//{
//    public class RootModule : ARootModule<RootModuleConfiguration>
//    {
//        public RootModule(IConfiguration configuration) : base(configuration)
//        {
//        }

//        public RootModule(Configuration.ConfigurationBuilder configurationBuilder) : base(configurationBuilder)
//        {
//        }

//        public RootModule(RootModuleConfiguration configuration, List<IModule> nestedModules = null) : base(configuration, nestedModules)
//        {
//        }

//        public RootModule(ConfigurationBuilder<RootModuleConfiguration> configurationBuilder, List<IModule> nestedModules = null) : base(configurationBuilder, nestedModules)
//        {
//        }

//        public RootModule(Action<ConfigurationBuilder<RootModuleConfiguration>> builderHandler, List<IModule> nestedModules = null) : base(builderHandler, nestedModules)
//        {
//        }
//    }
//}
