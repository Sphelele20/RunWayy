# Part 3 demonstration and submission

Use this as your final walkthrough, not a claim that cloud/container checks have already passed.

- Register Organiser and Participant accounts; show the different navigation and restricted pages.
- Create an upcoming event with every field and a banner; edit it.
- Add multiple categories; remove an unused one.
- Filter events as Participant, open details, select a category and enrol.
- Show the immediate Confirmed entry in My entries and the organiser's start list.
- After an event has concluded, publish a finish time and position as its owner; show My results as that participant.
- Edit the participant profile and upload a photo.
- Configure Azure, upload new images, show the private blobs and rendered pages.
- Run `docker compose up --build` on a machine with working Docker; demonstrate the same UI.
- Show passing CI for API, MVC and containers.
- Keep genuine, meaningful implementation history and check the rubric's minimum 20-commit requirement.
- Explain Web -> API -> database/blob flow, session authentication, ownership rules and Docker setup in your YouTube walkthrough.
- Put your actual YouTube link in the submission and submit the repository link in ARC yourself.

Do not expose storage keys or account passwords in the recording. Use test identities and a fresh development database. For a completed-event demonstration, create a short-lived test event beforehand and wait until its start time has passed; the UI will then enable result entry. Automated tests adjust only their isolated temporary database to exercise this case.
