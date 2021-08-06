# traefik-on-service-fabric
Azure Service Fabric now has support for Traefik! 

To add a cert for domains that are not *.thuzi.com, etc.
1. upload the cert to the vault "platform-production" on the azure portal. Note the name you apply it.
1. update the file located at : traefik-on-service-fabric/src/Traefik/ApplicationPackageRoot/TraefikPkg/Code/traefik.toml for your cert by adding (replace supercon in the example below with your event)
```
      [[entryPoints.https.tls.certificates]]
      certFile = "certs/supercon.crt"
      keyFile = "certs/supercon.key"
```
to the section `[entryPoints.https]` that begins on line 45

After you have done this, you need to update the octopus deploy traefik on service fabric project's variables: https://octo.thuzi.com/app#/Spaces-1/projects/servicefabric-traefik/variables

Please locate the environment for the variable 'TraefikCertsToConfigure'
add a comma to the end of the list, then your certificate to match the pattern
```
*supercon*;KeyVault;*floridasupercon-cert-prod*
```
<cert name from step 2>;KeyVault;<certificate name in vault recorded from step 1>
