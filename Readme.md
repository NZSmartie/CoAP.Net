# CoAP.Net

My attempt at writing a CoAP library for .Net Core that is compliant with [RFC7252]

No attempt will be made to make this library backwards complaint with its drafts

**Note:** This project was created with Visual Studio 2017 RC, thus older versions MSBuild/XBuild doesn't like the .csproj files

## Motivation

The idea behind this library is to encode and decode packets for the CoAP protocol that does not depend on any transport mechanisms. 

Minimising the need for platform dependant librarbies (i.e. System.Net) and providing the application layer the freedome to use its own transport. Since CoAP is designed for unreliable transport layers.

**Note**: I'm also following TDD as an exercise to become more familair with the workflow and get into the habbit of writing method stubs.

## Status

I will throw a checklist here when I figure out what actually plan on doing with this library

### Working

 - Creates and Decodes message packets with
   - Token
   - Opions
   - Paylaods

### Todo

 - Create unit tests to cover as much of RFC7252 as possible.
 - Message timeout 8 resend handler
 - Verify messages are valid and fail gracefully
 - An application layer friendly API (This is a very low level library at the time of writing)