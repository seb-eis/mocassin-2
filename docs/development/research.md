# Research notes

This page is the task discipline for delegated reading and fact-finding. Research answers a question from **primary
sources** and leaves a cited note the next context can reuse.

Use it for external API behaviour, library facts, standards, protocol details, licence questions, and prior art. Do
not use it to restate the repository; read the repository directly.

## 1. Rules

- **Primary sources first.** Official documentation, specifications, source code, first-party APIs, release notes,
  and standards. A secondary blog post may point to the source, but it is not the source. Use the web search and
  fetch tools when they are available, then read the relevant section rather than a search snippet.
- **Cite every non-obvious claim** with a URL, document title, version, and the section or line that supports it.
- **Separate fact from interpretation.** State what the source says, then what you conclude.
- **Record uncertainty.** If sources disagree or the version is unclear, say so instead of choosing silently.
- **Protect secrets.** Redact credentials, tokens, private URLs, and personal data, per
  [`agent-security.md`](agent-security.md).
- **Keep it bounded.** Answer the question asked; do not turn research into a broad web crawl.
- **Use the repository's convention.** Save the note under
  [`docs/planning/research/`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/research/README.md)
  with the template there, and link it from the step or record that uses it.

The lead, design agent, or dev may request research. A fresh context is useful for a large reading task but is not
required for a small lookup. The result is durable evidence, not a chat summary. The note is written by the design
agent or dev; a review agent cites sources in its assigned record and asks the lead for a research note rather than
writing outside its record.

## 2. Output

Create `docs/planning/research/<YYYY-MM-DD>-<slug>.md` from the
[template](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/research/_template.md). It must contain:

- the question;
- the answer, with confidence;
- the sources and what each supports, with version and section;
- what was not checked;
- the effect on the step, decision, or record that used it.

Link the note from the step page or the review/audit record that used it. A research note that nothing links is an
orphan, not evidence.

## 3. Anti-patterns

- answering from memory without a source;
- treating a search-result summary as a primary source;
- copying long passages instead of extracting the claim and the citation;
- mixing opinion into the factual section;
- saving the note outside the repository so the next context cannot find it;
- researching a question that the repository already answers;
- reading an untouched licence or version claim into the note without checking it.
