export const meta = {
  name: 'ship-and-watch',
  description: 'Commit and push pending changes, watch the triggered GitHub Actions run, and auto-diagnose/fix on failure',
  whenToUse: 'After making changes in this repo that are ready to ship: pushes, watches the resulting deploy-production.yml run, and loops a fix-and-reship attempt if it fails.',
  phases: [
    { title: 'Ship', detail: 'commit and push pending changes' },
    { title: 'Watch', detail: 'poll the triggered workflow run until it finishes' },
    { title: 'Fix', detail: 'on failure, diagnose and patch, then re-ship' },
  ],
}

const MAX_ATTEMPTS = 3
const SHIP_SCHEMA = {
  type: 'object',
  properties: {
    pushed: { type: 'boolean' },
    sha: { type: 'string' },
  },
  required: ['pushed'],
}
const WATCH_SCHEMA = {
  type: 'object',
  properties: {
    conclusion: { type: 'string' },
    failingJob: { type: 'string' },
    logExcerpt: { type: 'string' },
  },
  required: ['conclusion'],
}

let attempt = 0
let result = { status: 'unknown' }

while (attempt < MAX_ATTEMPTS) {
  attempt++

  phase('Ship')
  const ship = await agent(
    'Stage and commit any pending changes in this git repository with a clear, conventional commit message ' +
      '(only if there are changes to commit), then push the current branch to its upstream remote. ' +
      "If there is nothing new to commit but the branch is ahead of upstream, just push. " +
      "If there is nothing to commit and nothing to push, do neither. " +
      'Report whether you pushed and, if so, the exact commit SHA that is now on the remote.',
    { label: `ship-attempt-${attempt}`, phase: 'Ship', schema: SHIP_SCHEMA },
  )

  if (!ship?.pushed) {
    log('Nothing to push — nothing to watch.')
    result = { status: 'no-op' }
    break
  }

  phase('Watch')
  const watch = await agent(
    `Using the gh CLI, find the GitHub Actions workflow run triggered by commit ${ship.sha} in this repository ` +
      `(e.g. "gh run list --commit ${ship.sha} --limit 5"), then wait for it to finish ` +
      '(e.g. "gh run watch <run-id> --exit-status"). Report its final conclusion ("success" or "failure"). ' +
      'If it failed, also report the name of the failing job and the tail of its failing step\'s log ' +
      '(e.g. "gh run view <run-id> --log-failed").',
    { label: `watch-attempt-${attempt}`, phase: 'Watch', schema: WATCH_SCHEMA },
  )

  if (!watch || watch.conclusion === 'success') {
    result = { status: 'success', sha: ship.sha, attempts: attempt }
    break
  }

  log(`CI failed on attempt ${attempt}/${MAX_ATTEMPTS} — job: ${watch.failingJob || 'unknown'}`)

  phase('Fix')
  await agent(
    `The GitHub Actions run for commit ${ship.sha} failed in job "${watch.failingJob || 'unknown'}". ` +
      `Failure log excerpt:\n\n${watch.logExcerpt || '(no log captured)'}\n\n` +
      'Diagnose the root cause in this repository\'s source or workflow files and apply a real fix. ' +
      'Do not skip, disable, or work around the failing check — fix the underlying issue. ' +
      'Leave the fix staged/committed locally; do not push yourself, the next loop iteration will ship it.',
    { label: `fix-attempt-${attempt}`, phase: 'Fix' },
  )

  result = { status: 'retrying', attempts: attempt }
}

if (result.status === 'retrying') {
  log(`Gave up after ${MAX_ATTEMPTS} attempts — CI is still failing.`)
  result.status = 'failed'
}

return result
