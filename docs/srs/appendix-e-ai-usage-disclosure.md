# Appendix E — AI Usage Disclosure

The SE3090 module permits AI use at Level 4 (Full AI) during development, on condition that all use is disclosed, verified and understood, and prohibits any external AI assistance during the demonstration and viva. CoreGrid adopts the following operating rule without exception.

**Operating rule**

AI proposes → the owner reviews the diff → the owner tests it → the owner understands it → Git records it. Never: AI generates → copy → commit. Any artefact its named owner cannot explain, modify or debug is treated as not delivered, and is removed rather than submitted.

| Log field | What is recorded |
|---|---|
| Date | When the assistance was used. |
| Tool and model | The specific assistant and model version. |
| Task and section | The requirement identifier or document section the work relates to. |
| What the tool produced | A factual summary of the output received. |
| What was changed or rejected | The specific modifications made, and anything discarded, with the reason. |
| How it was verified | The test executed, the review performed or the behaviour observed that established correctness. |

Each student maintains this log individually in their section of the consolidated report, together with a one-page reflection on what the tools did well, what they got wrong, what was changed or rejected and why, and what the student learned about their own understanding. The group submits one consolidated declaration confirming that all AI use has been disclosed and that every member can explain, test and modify the work submitted under their name. No external AI assistant, chatbot, IDE copilot or agentic coding tool is used during the demonstration or viva; the only AI executed during the evaluation is CoreGrid's own agentic subsystem.


## Individual AI Usage Record — Jayashan Guruge

**Assigned Areas:**
Component A — Asset Registry and QR Identification; Backend Integration; Web Frontend; Mobile Application; Planner Agent.

### AI Usage Declaration

During the development of CoreGrid, I used AI-assisted development tools as permitted by the SE3090 Level 4 (Full AI) requirements. The primary tools used were **OpenAI ChatGPT — GPT-5.6 Luna** and **Claude Opus 5**.

AI assistance was used for technical explanations, debugging, implementation guidance, code review, testing guidance, configuration troubleshooting, and documentation. AI suggestions were treated as proposals rather than final solutions. I reviewed the existing code, compared suggestions with the project requirements, adapted or rejected unsuitable suggestions, and verified implemented changes through compilation, automated tests, API testing, logs, emulator testing, physical-device testing, and manual verification.

The following record documents individual AI-assisted activities related to my assigned work.

---

### Individual AI Usage Log

| Date | Tool and Model | Task and Section | What the Tool Produced | What Was Changed or Rejected | How It Was Verified |
|---|---|---|---|---|---|
| 2026-08-15 | ChatGPT — GPT-5.6 Luna | Backend — ASP.NET Core project structure | Explained the organisation of backend features, controllers, services, DTOs and models. | Adapted the suggested structure to the existing CoreGrid architecture. | Reviewed the project structure and verified with `dotnet build`. |
| 2026-08-15 | Claude Opus 5 | Backend — Dependency Injection | Helped explain service registration and dependency injection errors. | Applied only the registrations required by the existing services. | Verified using `dotnet build` and `dotnet run`. |
| 2026-08-15 | ChatGPT — GPT-5.6 Luna | Backend — API configuration | Assisted with ASP.NET Core API configuration and environment settings. | Adjusted configuration according to the actual development environment. | Verified by starting the API and testing an endpoint. |
| 2026-08-18 | Claude Opus 5 | Backend — Service interfaces | Assisted with understanding service/interface relationships. | Modified the implementation to match existing CoreGrid interfaces. | Verified through compilation. |
| 2026-08-19 | ChatGPT — GPT-5.6 Luna | Backend — Entity Framework Core | Explained EF Core entity and DbContext configuration. | Adapted the suggestions to the existing CoreGrid database model. | Verified through build and database operations. |
| 2026-08-21 | Claude Opus 5 | Backend — Database relationships | Assisted with understanding relationships between CoreGrid entities. | Retained only relationships compatible with the existing domain model. | Verified through EF Core and database testing. |
| 2026-08-21 | ChatGPT — GPT-5.6 Luna | Backend — Database migrations | Assisted with understanding migration commands and migration errors. | Adjusted migration steps according to the actual database state. | Verified by running migrations and checking the database. |
| 2026-08-22 | ChatGPT — GPT-5.6 Luna | Backend — Asset entity | Assisted with reviewing the Asset entity and related data. | Adapted suggestions to the existing CoreGrid domain model. | Verified through compilation and API responses. |
| 2026-08-22 | Claude Opus 5 | Backend — Asset service | Suggested approaches for asset service operations. | Modified the implementation to follow existing service conventions. | Verified using backend API calls. |
| 2026-08-24 | ChatGPT — GPT-5.6 Luna | Backend — Asset API endpoints | Assisted with understanding asset registration and retrieval endpoints. | Adjusted endpoint logic to match existing API contracts. | Verified through API requests. |
| 2026-08-25 | Claude Opus 5 | Backend — Asset lookup | Assisted with asset lookup API implementation. | Adapted the implementation to existing routes and DTOs. | Verified by requesting asset data through the API. |
| 2026-08-28 | ChatGPT — GPT-5.6 Luna | Backend — Asset validation | Assisted with request validation and DTO-related issues. | Modified validation according to existing request models. | Verified with API requests and build results. |
| 2026-08-27 | Claude Opus 5 | Backend — Exception handling | Helped interpret ASP.NET Core exceptions and API errors. | Rejected generic solutions where they did not address the actual error. | Verified by reproducing the exception and testing the correction. |
| 2026-08-28 | ChatGPT — GPT-5.6 Luna | Backend — Authorization | Explained role-based access and authorization requirements. | Adapted authorization logic to CoreGrid roles and permissions. | Tested with different user roles. |
| 2026-08-29 | Claude Opus 5 | Backend — Authentication | Assisted with understanding authentication configuration. | Adjusted configuration to the actual ThunderID setup. | Verified through authenticated API requests. |
| 2026-08-31 | ChatGPT — GPT-5.6 Luna | Backend — ThunderID integration | Assisted with ThunderID issuer, client and authentication configuration. | Modified values to match the actual development environment. | Verified through login and authenticated API calls. |
| 2026-08-31 | ChatGPT — GPT-5.6 Luna | Backend — Asset history | Assisted with understanding asset history operations. | Adapted the implementation to existing history models and API contracts. | Verified through API responses and database records. |
| 2026-09-01 | Claude Opus 5 | Backend — Verification API | Assisted with the backend verification workflow. | Modified suggestions according to existing verification models. | Verified through API testing. |
| 2026-09-01 | ChatGPT — GPT-5.6 Luna | Backend — Verification validation | Assisted with identifying validation requirements for verification requests. | Adjusted the implementation to existing request models. | Verified with valid and invalid API requests. |
| 2026-09-01 | ChatGPT — GPT-5.6 Luna | Backend — Planner Agent API integration | Explained communication between the Planner Agent and CoreGrid API. | Adapted the API call to the existing agent-tool endpoint. | Verified using API requests and application logs. |
| 2026-09-01 | ChatGPT — GPT-5.6 Luna | Backend — Planner Agent service token | Assisted with diagnosing `401 Unauthorized`. | Identified missing service-token configuration and updated the environment configuration. | Verified by successfully retrying the request. |
| 2026-09-05 | Claude Opus 5 | Backend — Planner Agent error handling | Assisted with interpreting Planner Agent API failures. | Distinguished configuration errors from external service issues. | Verified using logs and API responses. |
| 2026-09-06 | ChatGPT — GPT-5.6 Luna | Backend — HTTP 429 error | Assisted with understanding an external API rate/quota error. | Did not treat the external quota issue as an application-code defect. | Verified from the returned API response and logs. |
| 2026-09-07 | ChatGPT — GPT-5.6 Luna | Backend — HTTP 502 error | Assisted with tracing a Bad Gateway response. | Investigated the underlying service communication rather than applying a generic fix. | Verified through service logs and repeated requests. |
| 2026-09-08 | Claude Opus 5 | Backend — Storage service | Assisted with implementing S3-compatible object storage configuration. | Adapted the implementation to the CoreGrid storage requirements. | Verified through compilation and runtime testing. |
| 2026-09-08 | ChatGPT — GPT-5.6 Luna | Backend — Cloudflare R2 configuration | Assisted with understanding Cloudflare R2 configuration using the S3-compatible API. | Modified endpoint and configuration values to match the actual environment. | Verified by starting the application and testing storage functionality. |
| 2026-09-10 | ChatGPT — GPT-5.6 Luna | Backend — Duplicate configuration | Assisted with identifying duplicate configuration/property registration. | Removed or modified the conflicting definition after reviewing the existing implementation. | Verified using `dotnet build`. |
| 2026-09-10 | Claude Opus 5 | Backend — API response handling | Assisted with reviewing API response structures and error responses. | Adjusted response handling to match existing API contracts. | Verified through API testing. |
| 2026-09-10 | ChatGPT — GPT-5.6 Luna | Backend — Runtime debugging | Assisted with interpreting backend runtime logs. | Changed only the implementation related to the observed error. | Verified by reproducing and resolving the runtime issue. |
| 2026-09-13 | Claude Opus 5 | Backend — Backend build debugging | Assisted with analysing compiler errors. | Corrected the relevant implementation instead of applying unrelated changes. | Verified using `dotnet build`. |
| 2026-09-14 | ChatGPT — GPT-5.6 Luna | Backend — CI build failure | Assisted with interpreting GitHub Actions backend build failures. | Updated only the confirmed source of the failure. | Verified through local build and CI execution. |
| 2026-09-16 | ChatGPT — GPT-5.6 Luna | Backend — API testing | Suggested approaches for manually testing API endpoints. | Adapted tests to the actual CoreGrid endpoints. | Verified using API requests and responses. |
| 2026-09-16 | ChatGPT — GPT-5.6 Luna | Frontend — React asset components | Assisted with asset-related React components. | Modified components to follow existing frontend conventions. | Verified in the Vite development environment. |
| 2026-09-16 | Claude Opus 5 | Frontend — Asset listing | Assisted with asset listing UI and API integration. | Adapted the implementation to the actual API response structure. | Verified through browser testing. |
| 2026-09-16 | ChatGPT — GPT-5.6 Luna | Frontend — Asset detail page | Assisted with asset detail UI and actions. | Modified the UI to fit existing CoreGrid components. | Verified through manual browser testing. |
| 2026-09-19 | ChatGPT — GPT-5.6 Luna | Frontend — Routing | Assisted with React routing for asset and verification features. | Added/adjusted routes according to the existing application structure. | Verified by navigating through the application. |
| 2026-09-20 | Claude Opus 5 | Frontend — API integration | Assisted with frontend API calls. | Modified API calls to match actual backend routes and responses. | Verified using browser network requests. |
| 2026-09-21 | ChatGPT — GPT-5.6 Luna | Frontend — Role-based UI | Assisted with role-specific feature visibility. | Adjusted UI behaviour according to CoreGrid permissions. | Verified with different user roles. |
| 2026-09-22 | ChatGPT — GPT-5.6 Luna | Frontend — Verification actions | Assisted with asset verification actions and navigation. | Adjusted the action flow to the actual verification requirements. | Verified by testing the verification navigation. |
| 2026-09-23 | ChatGPT — GPT-5.6 Luna | Frontend — UI improvements | Suggested improvements to cards, buttons, spacing and dashboard presentation. | Selected and modified suitable suggestions to match the project UI. | Verified through browser testing and visual inspection. |
| 2026-09-24 | ChatGPT — GPT-5.6 Luna | Frontend — React test failures | Assisted with interpreting Testing Library failures. | Modified implementation/test assumptions based on actual rendered output. | Verified by rerunning the frontend tests. |
| 2026-09-25 | ChatGPT — GPT-5.6 Luna | Frontend — Permission-related test | Assisted with a role-based UI test failure. | Updated the implementation/test based on the actual permission requirement. | Verified by rerunning the test suite. |
| 2026-09-16 | ChatGPT — GPT-5.6 Luna | Mobile — Flutter configuration | Assisted with Flutter environment and API configuration. | Adapted configuration to the local Android development environment. | Verified using `flutter run`. |
| 2026-09-18 | ChatGPT — GPT-5.6 Luna | Mobile — Flutter API client | Assisted with API client configuration and request handling. | Modified the client to match the CoreGrid API. | Verified through mobile API requests. |
| 2026-09-18 | ChatGPT — GPT-5.6 Luna | Mobile — ThunderID authentication | Assisted with Flutter authentication and AppAuth configuration. | Adjusted issuer and local configuration to the actual environment. | Verified through mobile login. |
| 2026-09-18 | ChatGPT — GPT-5.6 Luna | Mobile — Asset lookup | Assisted with asset lookup screen implementation. | Adapted the implementation to the existing Riverpod architecture. | Verified on Android emulator and device. |
| 2026-09-20 | ChatGPT — GPT-5.6 Luna | Mobile — Asset detail | Assisted with asset detail screen behaviour and navigation. | Modified UI/navigation to match existing mobile requirements. | Verified through device testing. |
| 2026-09-21 | ChatGPT — GPT-5.6 Luna | Mobile — QR scanning | Assisted with QR scanning and asset identification. | Adapted the scanning flow to the actual CoreGrid API. | Verified by scanning asset QR codes. |
| 2026-09-22 | ChatGPT — GPT-5.6 Luna | Mobile — Verification task screen | Assisted with verification task navigation. | Adjusted navigation according to the existing workflow. | Verified through mobile testing. |
| 2026-09-23 | ChatGPT — GPT-5.6 Luna | Mobile — Ad-hoc verification | Assisted with identifying the missing route/entry point for `AssetVerificationScreen`. | Connected the screen to the appropriate navigation flow. | Verified by testing the route and verification flow. |
| 2026-09-24 | ChatGPT — GPT-5.6 Luna | Mobile — Android emulator | Assisted with Flutter installation and debug connection issues. | Adjusted the development setup according to the actual emulator state. | Verified by running the application on the emulator. |
| 2026-09-25 | ChatGPT — GPT-5.6 Luna | Mobile — Physical Android device | Assisted with wireless ADB configuration. | Adapted the connection commands to the actual device. | Verified using `adb devices` and device testing. |
| 2026-09-25 | ChatGPT — GPT-5.6 Luna | Mobile — ADB reverse networking | Assisted with forwarding local backend ports to the Android device. | Applied the required `adb reverse` configuration. | Verified through mobile API requests. |
| 2026-09-26 | ChatGPT — GPT-5.6 Luna | Mobile — Local HTTPS development | Assisted with understanding local development certificate warnings. | Used the appropriate development configuration rather than treating the warning as a production certificate issue. | Verified through local API connectivity. |
| 2026-09-10 | ChatGPT — GPT-5.6 Luna | QR — QR generation | Assisted with QR generation implementation. | Adapted the implementation to the available project dependencies. | Verified by generating QR output. |
| 2026-09-11 | ChatGPT — GPT-5.6 Luna | QR — QR asset resolution | Assisted with the QR-to-asset resolution flow. | Adjusted the flow to match the actual API route. | Verified by scanning and resolving an asset. |
| 2026-09-12 | ChatGPT — GPT-5.6 Luna | QR — QR mobile flow | Assisted with integrating QR scanning into the mobile application. | Adapted navigation and API communication to the existing app. | Verified on an Android device. |
| 2026-09-13 | Claude Opus 5 | Planner Agent — FastAPI setup | Assisted with understanding the FastAPI application structure. | Adapted commands and configuration to the existing project. | Verified through the FastAPI server and Swagger UI. |
| 2026-09-14 | ChatGPT — GPT-5.6 Luna | Planner Agent — Agent tools | Assisted with the Python tool used to communicate with CoreGrid APIs. | Adapted the tool to the actual endpoint and authentication requirements. | Verified through API requests. |
| 2026-09-15 | ChatGPT — GPT-5.6 Luna | Planner Agent — Asset summary | Assisted with understanding the asset summary endpoint used by the agent. | Adjusted the tool implementation to match the backend response. | Verified through agent requests. |
| 2026-09-16 | ChatGPT — GPT-5.6 Luna | Planner Agent — Service authentication | Assisted with configuring the service token. | Added the required configuration after checking the actual environment. | Verified after the `401` error was resolved. |
| 2026-09-17 | ChatGPT — GPT-5.6 Luna | Planner Agent — Error analysis | Assisted with interpreting `401`, `429` and `502` errors. | Distinguished application errors from external service/quota problems. | Verified through logs and API responses. |
| 2026-09-18 | ChatGPT — GPT-5.6 Luna | Testing — Backend | Assisted with interpreting backend test/build failures. | Modified implementation based on actual test output. | Verified by rerunning tests/builds. |
| 2026-09-19 | ChatGPT — GPT-5.6 Luna | Testing — Frontend | Assisted with frontend test failures. | Corrected implementation/test assumptions after checking actual UI behaviour. | Verified by rerunning frontend tests. |
| 2026-09-20 | ChatGPT — GPT-5.6 Luna | Testing — Flutter | Assisted with Flutter widget and application testing issues. | Adapted implementation according to the actual widget structure. | Verified using Flutter tests and device testing. |
| 2026-09-21 | ChatGPT — GPT-5.6 Luna | Git — Feature branch workflow | Explained feature branch and development branch workflow. | Applied the workflow to the team's actual repository. | Verified through Git branch and pull-request operations. |
| 2026-09-22 | ChatGPT — GPT-5.6 Luna | Git — Merge conflicts | Explained current/incoming changes during merge conflict resolution. | Selected changes after comparing the actual code rather than accepting blindly. | Verified with build and tests after merging. |
| 2026-09-23 | ChatGPT — GPT-5.6 Luna | CI — GitHub Actions | Assisted with interpreting CI failures. | Fixed only the issues confirmed by CI output. | Verified through subsequent CI runs. |
| 2026-09-24 | ChatGPT — GPT-5.6 Luna | Documentation — Technical explanations | Assisted with explaining technical implementation details for project documentation. | Reviewed and simplified explanations to accurately represent my implementation. | Compared the documentation against the actual code. |
| 2026-09-25 | ChatGPT — GPT-5.6 Luna | Documentation — Development notes | Assisted with organising development notes and troubleshooting records. | Kept only information relevant to the actual development work. | Cross-checked against project files and development results. |
| 2026-09-26 | ChatGPT — GPT-5.6 Luna | Backend — Final configuration review | Assisted with reviewing remaining backend configuration issues. | Corrected only confirmed configuration problems. | Verified using `dotnet build`, `dotnet run`, logs and API testing. |


---

### Individual Reflection

AI assistance played a supporting role throughout my development of CoreGrid. I used ChatGPT and Claude Opus 5 to help understand technical problems, investigate errors, review possible implementation approaches, and improve my understanding of different technologies used in the project.

For the **backend**, AI assistance was used with ASP.NET Core, dependency injection, Entity Framework Core, database migrations, API development, validation, authentication, authorization, storage configuration, error handling, and Planner Agent integration.

For the **web frontend**, AI assistance was used with React, Vite, API integration, routing, asset pages, role-based UI behaviour, verification actions, UI improvements, and frontend testing.

For the **mobile application**, AI assistance supported Flutter configuration, API integration, ThunderID authentication, asset lookup, asset details, QR scanning, verification flows, Android emulator debugging, physical-device testing, and ADB networking.

For the **Planner Agent**, AI assistance was used to understand FastAPI, agent tools, CoreGrid API communication, service authentication, asset-summary requests, and error handling.

AI suggestions were not accepted without review. I compared proposed solutions with the existing CoreGrid architecture, source code, API contracts, database structure, project requirements, and development environment. When a suggestion was incorrect or incompatible, it was modified or rejected.

I verified changes using `dotnet build`, `dotnet run`, API requests, database checks, frontend tests, Flutter tests, GitHub Actions, application logs, Android emulator testing, physical-device testing, and manual functional testing.

The use of AI helped me improve my understanding of the technologies and the relationships between the CoreGrid backend, web frontend, mobile application, authentication system, database, QR identification functionality, verification workflows, and Planner Agent.

I remain responsible for the final implementation submitted under my name. I have reviewed the AI-assisted work and understand the relevant implementation sufficiently to explain, modify, test, and debug it.

AI assistance was used during development in accordance with the **SE3090 Level 4 (Full AI)** requirements. No external AI assistant, chatbot, IDE copilot, or agentic coding tool was used during the demonstration or viva. The only AI executed during the evaluation was CoreGrid's own agentic subsystem.

---

### Declaration

I confirm that:

- AI-assisted development was disclosed in this record.
- AI-generated suggestions were reviewed before implementation.
- I tested the relevant changes after implementation.
- I understand the functionality submitted under my name.
- I can explain, modify, and debug the relevant implementation.
- Unsuitable or incorrect AI suggestions were modified or rejected.
- No external AI assistant was used during the demonstration or viva.
- The final submitted implementation remains my responsibility.

**Student:** Jayashan Guruge
**Project:** CoreGrid
**Module:** SE3090
**Assigned Areas:** Asset Registry, QR Identification, Backend Integration, Web Frontend, Mobile Application, Planner Agent
---

*End of Software Requirements Specification — CoreGrid, Version 1.0.*
