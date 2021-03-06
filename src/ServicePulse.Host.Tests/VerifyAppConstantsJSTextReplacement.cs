﻿namespace ServicePulse.Host.Tests
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    [TestFixture]
    public class VerifyAppConstantsJSTextReplacement
    {
        // app.constants.js holds the URL used to connect to SC
        // this both user configurable and updated during installation
        Regex sc_url_regex = new Regex(@"(service_control_url\s*\:\s*['""])(.*?)(['""])");
        Regex version_regex = new Regex(@"(version\s*\:\s*['""])(.*?)(['""])");

        [Test]
        public void app_constants_js_validation()
        {
            var pathToConfig = Path.Combine(TestContext.CurrentContext.TestDirectory, "app.constants.js");
            Assert.IsTrue(File.Exists(pathToConfig), "app.constants.js does not exist - this will break installation code");

            var config = File.ReadAllText(pathToConfig);
            var matchUrl = sc_url_regex.Match(config);
            Assert.IsTrue(matchUrl.Success, "regex failed to match app.constant.js for SC URI update");
            Uri uri;

            Assert.IsTrue(Uri.TryCreate(matchUrl.Groups[2].Value, UriKind.Absolute, out uri), "regex match found in app.constants.js is not a valid URI");
            var matchVersion = version_regex.Match(config);
            Assert.IsTrue(matchVersion.Success, "regex failed to match app.constant.js for the version string");
        }

        [Test]
        public void replace_version_regex_tests()
        {
            var configSnippets = new Dictionary<string, (string ConfigSnippet, Regex VersionRegex)>()
            {
                {
                    "1.3.0",
                    (
                        ConfigSnippet: @"angular.module('sc')
                            .constant('version', '1.3.0')
                            .constant('scConfig';",
                        VersionRegex: new Regex(@"(constant\s*\(\s*'version'\s*,\s*['""])(.*?)(['""])")
                    )
                },
                {
                    "1.3.0-beta1",
                    (
                        ConfigSnippet: @"angular.module('sc')
                            .constant ( 'version' , '1.3.0-beta1')
                            .constant('scConfig';",
                        VersionRegex: new Regex(@"(constant\s*\(\s*'version'\s*,\s*['""])(.*?)(['""])")
                    )
                },
                {
                    "",
                    (
                        ConfigSnippet: @"angular.module('sc')
                            .constant('version' , '' )
                            .constant('scConfig';",
                        VersionRegex: new Regex(@"(constant\s*\(\s*'version'\s*,\s*['""])(.*?)(['""])")
                    )
                },
                {
                    "1.20.0",
                    (
                        ConfigSnippet: @"window.defaultConfig = {
                                version: '1.20.0',
                                service_control_url: '
                            };",
                        VersionRegex: new Regex(@"(version\s*\:\s*['""])(.*?)(['""])")
                    )
                },
            };

            foreach (var config in configSnippets)
            {
                var expectedResult = config.Key;
                var text = config.Value.ConfigSnippet;

                var match = config.Value.VersionRegex.Match(text);
                Assert.IsTrue(match.Success, "regex failed to match version string");
                Assert.IsTrue(match.Groups[2].Value.Equals(expectedResult), string.Format("Version regex did not return expected value which was {0}", expectedResult));
            }
        }

        [Test]
        public void test_regex_match_against_config_variants()
        {
            var configVariations = new[]
            {
                // Standard 1.20.0 config            
                @"window.defaultConfig = {
                    default_route: '/dashboard',
                    version: '1.20.0',
                    service_control_url: 'http://localhost:33333/api/',
                    monitoring_urls: ['http://localhost:33633/']
                };
                ",
                // Standard 1.3 config            
                @"angular.module('sc')
                    .constant('version', '1.3.0')
                    .constant('scConfig', {
                        service_control_url:'http://localhost:33333/api/',
                        service_pulse_url: 'https://platformupdate.particular.net/servicepulse.txt'
                    });
                ",
                // Added whitespace and custom FQDN
                @"angular.module('sc')
                    .constant('version', '1.3.0')
                    .constant('scConfig', {
                        service_control_url :  'http://host.network.com:33333/api/'  ,
                        service_pulse_url: 'https://platformupdate.particular.net/servicepulse.txt'
                });
                ",
                // Line breaks and flip urls
                @"angular.module('sc')
                    .constant('version', '1.3.0')
                    .constant('scConfig', {
                    service_pulse_url: 
                            'https://platformupdate.particular.net/servicepulse.txt',
                            service_control_url :
                            'http://localhost:33333/api/'
                    });
                ",
                // 1.1 Config (config.js)
                @"
                    'use strict';
                    var SC = SC || {};
                    SC.config = {
                        service_control_url: 'http://localhost:33333/api/'
                    };
                ",

                // 1.1 Config custom URL(config.js)
                @"
                    'use strict';
                    var SC = SC || {};
                    SC.config = {
                        service_control_url: 'http://gb-dev:33333/api/'
                    };
                ",
                // Double Quotes instead of single
                @"
                    'use strict';
                    var SC = SC || {};
                    SC.config = {
                        service_control_url:""http://localhost:33333/api/\""
                    };
                "};

            for (var i = 0; i < configVariations.Length; i++)
            {
                var config = configVariations[i];
                var match = sc_url_regex.Match(config);
                Assert.IsTrue(match.Success, string.Format("regex failed on config variation {0} ", i));
                Uri uri;
                Assert.IsTrue(Uri.TryCreate(match.Groups[2].Value, UriKind.Absolute, out uri), string.Format("regex match in did not return a URI in config variation {0}", i));
            }
        }
    }
}
