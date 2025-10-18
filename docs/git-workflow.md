## Morning: 
Update local main, rebase your branch (Every day when you start)
```bash
git fetch origin                 # get remote updates
git switch main
git pull --ff-only               # fast-forward your local main
```

Start a task (new branch)
```
git switch -c <branch_name>
```
...edit code…

## During the day: 
Commit in small, meaningful chunks; push whenever:
> You reached a checkpoint,
> You want CI to run,
> You want your teammate to see progress,
> You just want a safe remote backup.

```git
git add -A
git commit -m "feat: draw colored segments on map"
```
First push of branch: `git push -u origin <branch_name>`
Else: `git push`


Keep your branch in sync with latest main
```git
git fetch origin
git merge origin/main
```
Resolve any conflicts if needed.

## Open a PR (recommended)
On GitHub: compare <branch_name> → into main.
After review & merge, delete the branch (GitHub offers a button).

## Start the next task
```git
git switch main
git pull --ff-only
git switch -c  <branch_name>
Quick sanity checks
git status       # what changed locally
git branch -vv   # what branch you’re on + upstream
git remote -v    # where “origin” points
```


