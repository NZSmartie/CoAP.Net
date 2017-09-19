---
title: CoAP.Net Documentation
---
# CoAP.Net Documentation 

[![Build status](https://ci.appveyor.com/api/projects/status/ku2x7p5eo2yf4lls?svg=true)](https://ci.appveyor.com/project/NZSmartie/coap-net-iu0to) [![Coverage Status](https://coveralls.io/repos/github/NZSmartie/CoAP.Net/badge.svg?branch=master)](https://coveralls.io/github/NZSmartie/CoAP.Net?branch=master) [![NuGet](https://img.shields.io/nuget/v/NZSmartie.CoAPNet.svg)](https://www.nuget.org/packages/NZSmartie.CoAPNet/) [![license](https://img.shields.io/github/license/NZSmartie/CoAP.Net.svg)](https://github.com/NZSmartie/CoAP.Net/blob/master/LICENSE)

[Changelog](../Changelog.md) | [View in Github](https://github.com/NZSmartie/CoAP.Net)

## About

This library encodes and decodes CoAP protocol packets that is transport agnostic. 
IT also provides a CoapClient and CoapServer for communicating over CoAP

Since CoAP is designed for unreliable transport layers. (6LoWPAN, UDP, etc...) it made sense to not worry about the transport implementaions and allow the applicatrion to provide their own.

If you're after a UDP transport example, see [CoAPNet.Udp](https://github.com/NZSmartie/CoAP.Net/tree/master/src/CoAPNet.Udp) ([![NuGet](https://img.shields.io/nuget/v/NZSmartie.CoAPNet.Udp.svg)](https://www.nuget.org/packages/NZSmartie.CoAPNet.Udp/))