//using Baubit.Configuration;
//using Microsoft.Extensions.Configuration;
//using System;
//using System.Collections.Generic;

//namespace Baubit.DI
//{
//    public abstract class ARootModule<TConfiguration> : AModule<TConfiguration> where TConfiguration : ARootModuleConfiguration
//    {
//        protected ARootModule(IConfiguration configuration) : base(configuration)
//        {
//        }

//        protected ARootModule(Configuration.ConfigurationBuilder configurationBuilder) : base(configurationBuilder)
//        {
//        }

//        protected ARootModule(TConfiguration configuration, List<IModule> nestedModules = null) : base(configuration, nestedModules)
//        {
//        }

//        protected ARootModule(ConfigurationBuilder<TConfiguration> configurationBuilder, List<IModule> nestedModules = null) : base(configurationBuilder, nestedModules)
//        {
//        }

//        protected ARootModule(Action<ConfigurationBuilder<TConfiguration>> builderHandler, List<IModule> nestedModules = null) : base(builderHandler, nestedModules)
//        {
//        }
//    }
//}
