
//
// InstrumentedClientElement.cs
//
// Author:
//    Tomas Restrepo (tomas@winterdom.com)
//

using System;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceModel.Configuration;
using System.Text;

namespace QuickCounters.Wcf.Configuration
{

   /// <summary>
   /// Configuration Element for the InstrumentedClientBehavior
   /// for Windows Communication Foundation
   /// </summary>
   public class InstrumentedClientElement : BehaviorExtensionElement
   {
      private const string SERVICE_PS = "service";
      private ConfigurationPropertyCollection _properties;

      /// <summary>
      /// Service prefix to use instead of the name
      /// of the service class
      /// </summary>
      [ConfigurationProperty(SERVICE_PS)]
      public string Service
      {
         get { return (string)base[SERVICE_PS]; }
         set { base[SERVICE_PS] = value; }
      }

      /// <summary>
      /// Return the type of the behavior we configure
      /// </summary>
      public override Type BehaviorType
      {
         get { return typeof(InstrumentedClientBehavior); }
      }


      /// <summary>
      /// Return a collection of all our properties
      /// </summary>
      protected override ConfigurationPropertyCollection Properties
      {
         get
         {
            if ( _properties == null )
            {
               _properties = new ConfigurationPropertyCollection();
               _properties.Add(new ConfigurationProperty(
                  SERVICE_PS, typeof(string), "",
                  ConfigurationPropertyOptions.None
                  ));
            }
            return _properties;
         }
      }

      /// <summary>
      /// Create an instance of the behavior
      /// we represent
      /// </summary>
      /// <returns>The InstrumentedClientBehavior instance</returns>
      protected override object CreateBehavior()
      {
         InstrumentedClientBehavior ins =
            new InstrumentedClientBehavior();
         ins.Service = Service;
         return ins;
      }

      /// <summary>
      /// Copy the information of another element into
      /// ourselves
      /// </summary>
      /// <param name="from">The element from which to copy</param>
      public override void CopyFrom(ServiceModelExtensionElement from)
      {
         base.CopyFrom(from);
         InstrumentedClientElement element = 
            (InstrumentedClientElement)from;
         Service = element.Service;
      }

   } // class InstrumentedClientElement

} // namespace QuickCounters.Wcf.Configuration

