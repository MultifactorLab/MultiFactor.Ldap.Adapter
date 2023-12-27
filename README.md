[![License](https://img.shields.io/badge/license-view-orange)](LICENSE.md)

# MultiFactor.Ldap.Adapter

_Also available in other languages: [Русский](README.ru.md)_

**MultiFactor Ldap Adapter** is a LDAP proxy server for Linux. It allows you to quickly add multifactor authentication to your applications with LDAP authentication.

The component is a part of <a href="https://multifactor.pro/" target="_blank">MultiFactor</a> 2FA hybrid solution. It is available with the source code and distributed for free.

* <a href="https://github.com/MultifactorLab/MultiFactor.Ldap.Adapter" target="_blank">Source code</a>
* <a href="https://github.com/MultifactorLab/MultiFactor.Ldap.Adapter/releases" target="_blank">Build</a>

Linux version of the component is available in <a href="https://github.com/MultifactorLab/multifactor-ldap-adapter" target="_blank">multifactor-ldap-adapter</a> repository.

See <a href="https://multifactor.pro/docs/ldap-adapter/windows/" target="_blank">knowledge base</a> for additional guidance on integrating 2FA through LDAP into your infrastructure.

## Table of Contents

- [Overview](#overview)
  - [Component Features](#component-features)
  - [Use Cases](#use-cases)
- [Prerequisites](#prerequisites)
- [Configuration](#configuration)
  - [General Parameters](#general-parameters)
- [Start-Up](#start-up)
- [Logs](#logs)
  - [Syslog](#syslog)
- [Certificate for TLS encryption](#certificate-for-tls-encryption)
- [License](#license)

## Overview

### Component Features

Key functionality:

- Proxying network traffic through LDAP protocol;
- Searching for authentication requests and confirming access on the user's phone with the second factor.

Key features:

- LDAP and LDAPS (encrypted TLS channel) support;
- Interception of authentication requests that use Simple, Digital, NTLM mechanisms;
- Bypassing requests from service accounts (Bind DN) without the second factor;
- Logging to Syslog server or SIEM system.

### Use Cases

Use LDAP Adapter Component to implement the following scenarios:

* Add a second authentication factor to applications connected to Active Directory or other LDAP directories;
* Enable traffic encryption for applications that do not support encrypted TLS connection.

## Prerequisites

- Component is installed on Windows Server starting from 2008 R2;
- Minimum server requirements: 2 CPUs, 4 GB RAM, 40 GB HDD (to run the OS and adapter for 100 simultaneous connections &mdash; about 1500 users);
- TCP ports 389 (LDAP) and 636 (LDAPS) must be open on the server to receive requests from clients;
- The server with the component installed needs access to ```api.multifactor.ru``` via TCP port 443 (TLS) or via HTTP proxy;
- To interact with Active Directory, the component needs access to the domain server via TCP port 389 (LDAP) or 636 (LDAPS);
- To write logs to Syslog, access to the Syslog server is required;
> For Windows Server versions prior to 2016, install <a href="https://www.microsoft.com/en-US/download/details.aspx?id=53344" target="_blank">Microsoft .NET Framework 4.6.2</a>.

## Configuration

The component's parameters are stored in ```MultiFactor.Ldap.Adapter.exe.config``` in XML format.

### General Parameters

```xml
<!-- The address and port (TCP) on which the adapter will listen to LDAP requests -->
<!-- If you specify 0.0.0.0, then the adapter will listen on all network interfaces -->
<add key="adapter-ldap-endpoint" value="0.0.0.0:389"/>

<!-- The address and port (TCP) on which the adapter will listen for LDAPS encrypted requests -->
<!-- If you specify 0.0.0.0, then the adapter will listen on all network interfaces -->
<add key="adapter-ldaps-endpoint" value="0.0.0.0:636"/>

<!-- Active Directory domain address or name, and ldap or ldaps connection scheme -->
<add key="ldap-server" value="ldaps://domain.local"/>

<!-- List of service accounts that do not require a second factor, separated by semicolons -->
<add key="ldap-service-accounts" value="CN=Service Acc,OU=Users,DC=domain,DC=local"/>


<!-- Multifactor API address -->
<add key="multifactor-api-url" value="https://api.multifactor.ru"/>
<!--Timeout for requests in the Multifactor API, the minimum value is 65 seconds-->
<add key="multifactor-api-timeout" value="00:01:05"/>
<!-- NAS-Identifier parameter to connect to the Multifactor API - from resource details in your account -->
<add key="multifactor-nas-identifier" value=""/>
<!-- Shared Secret parameter to connect to the Multifactor API - from resource details in your account -->
<add key="multifactor-shared-secret" value=""/>

<!-- Access to the Multifactor API via HTTP proxy (optional) -->
<!--add key="multifactor-api-proxy" value="http://proxy:3128"/-->

<!-- Logging level: 'Debug', 'Info', 'Warn', 'Error' -->
<add key="logging-level" value="Debug"/>
<!--certificate password leave empty or null for certificate without password-->
<!--<add key="certificate-password" value="XXXXXX"/>-->
```

## Start-Up

The component can run in console mode or as a Windows service. To run in console mode, just run the application.

To install it as a Windows Service, start it with the ```/i``` key as the Administrator
```shell
MultiFactor.Ldap.Adapter.exe /i
```
The component is installed in auto-startup mode by default on behalf of ```Network Service```.

To remove the Windows Service run with the ```/u``` key as Administrator
```shell
MultiFactor.Ldap.Adapter.exe /u
```

## Logs

Component's logs are located in the ```Logs``` folder. If they are not there, make sure that the folder is writable by the ```Network Service``` user.

### Syslog

```xml
<!--Server address and port, protocol can be either udp or tcp-->
<add key="syslog-server" value="udp://syslog-server:514"/>
<!--Log format: RFC3164 or RFC5424-->
<add key="syslog-format" value="RFC5424"/>
<!--Category: User, Auth, Auth2, Local0 .. Local7-->
<add key="syslog-facility" value="Auth"/>
<!--Application name (tag)-->
<add key="syslog-app-name" value="multifactor-ldap"/>
```

## Certificate for TLS Encryption

If the LDAPS scheme is enabled, the adapter creates a self-signed SSL certificate the first time it starts up, and saves it in the /tls folder in pfx format without a password.
This certificate will be used for server authentication and traffic encryption. You can replace it with your own certificate if necessary.

## License

Please note, the [license](LICENSE.md) does not entitle you to modify the source code of the Component or create derivative products based on it. The source code is provided as-is for evaluation purposes.
