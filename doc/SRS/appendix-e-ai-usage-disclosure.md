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

---

*End of Software Requirements Specification — CoreGrid, Version 1.0.*
