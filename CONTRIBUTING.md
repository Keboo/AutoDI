Contributing
============

* [Code](#code)
  * [Code style](#code-style)
  * [Dependencies](#dependencies)
  * [Unit tests](#unit-tests)
  * [Environment](#environment)
* [Contributing process](#contributing-process)
  * [Submitting a Pull Request](#Submitting-a-Pull-Request)
  * [Respond to feedback on pull request](#respond-to-feedback-on-pull-request)

## Code
### Code style

Normal .NET coding guidelines apply.
See the [Framework Design Guidelines](https://msdn.microsoft.com/en-us/library/ms229042%28v=vs.110%29.aspx) for more information.

### Dependencies

Additional dependencies should only be added after a discussion. If you believe a new dependnecy is required please ask in an issue, before submitting a PR. 

### Unit tests

Make sure to run all unit tests before creating a pull request.
Where aplicable please add unit tests. PR that simply improve the unit test coverage are happily accepted.

### Environment

AutoDI uses the latest C# and Visual Studio. If you are experiencing compile issue please verify that you are using the latest.

## Contributing process

 * If you have a feature request please open a new issue so we can discuss it.
 * If you have questions or problems, consider posting them in the [gitter chat](https://gitter.im/AutoDIContainer/Lobby).
 * If there is an existing issue marked [Help wanted](/issues?q=is%3Aissue+is%3Aopen+label%3A"help+wanted") that you want to work on please assign it to yourself so others know you are working on it. If it has been longer than a week, please post an additional comment indicating if you are still working on the issue or not so others know if they should work on it. If an issue goes longer than a week without a response, it may be unassigned so others can work on it.
 
 
### Submitting a Pull Request

 * Fork the repository
 * Create a branch named specific to the feature. You will need to create a separate branch for each issue you fix.
 * In the branch you do work specific to the feature.
 * Keep the changes specific to the bug or feature you are working on
 * Run the unit tests - AppVeyor will do this as well when you submit a PR. PRs will not be accepted until all of the unit tests pass.
 * Create a PR from your branch back to the master branch. Make sure to reference the issue you are fixing.

### Respond to feedback on pull request

If you would like feedback on work in progress, feel free to submit a PR prefixed with "WIP" so it is clear the work is not finished.
After a code review there may be some desired changes. This is simply to try and maintain a nice cohesive code base. 
