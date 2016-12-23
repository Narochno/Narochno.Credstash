# Narochno.Credstash

C# Implementation of Credstash

* Retrieve secrets for your applications in C#!
* ASP.NET Core IConfiguration provider.

Intended use is with the Credstash command line tool.  Use CLI to enter your values.  Configuration Provider is used to retrieve.

## What is it?

CredStash is a very simple, easy to use credential management and distribution system that uses AWS Key Management Service (KMS) for key wrapping and master-key storage, and DynamoDB for credential storage and sharing.

Many more details on the original:

* [Intro Blog post](https://blog.fugue.co/2015-04-21-aws-kms-secrets.html)
* [Credstash Repo](https://github.com/fugue/credstash) in Python

## Credstash vs Hashicorp Vault

Reference: [Credstash](https://github.com/fugue/credstash/issues/60)

Vault is really neat and they do some cool things (dynamic secret generation, key-splitting to protect master keys, etc.), but there are still some reasons why you might pick credstash over vault:

* Nothing to run. If you want to run vault, you need to run the secret storage backend (consul or some other datastore), you need to run the vault server itself, etc. With credstash, there's nothing to run. all of the data and key storage is handled by AWS services
* lower cost for a small number of secrets. If you just need to store a small handful of secrets, you can easilly fit the credstash DDB table in the free tier, and pay ~$1 per month for KMS. So you get good secret management for about a buck a month.
* Simple operations. Similar to "nothing to run", you dont need to worry about getting a quorum of admins together to unseal your master keys, dont need to worry about monitoring, runbooks for when the secret service goes down, etc. It does expose you to risk of AWS outages, but if you're running on AWS, you have that anyway

That said, if you want to do master key splitting, are not running on AWS, care about things like dynamic secret generation, have a trust boundary that's smaller than an instance, or want to use something other than AWS creds for AuthN/AuthZ, then vault may be a better choice for you.
