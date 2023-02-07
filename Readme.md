# Honey

Honey is a distributed platform. It allows you to execute multiple 
tasks (aka processes) through multiple Bees (aka machines).

Honey parallel and sequence tasks around jobs to create execution
workflows on a fully distributed platform.

Honey provides a web API to the end user as well as a web application 
on which you can follow and monitor workflow progresses and 
the state of your distributed platform.

Honey is a multi-users platform. To segregate the platform resources 
between users, each workflow is attached to a Beehive.

## Colony

A Colony hosts Bees and split them between Beehives to manage their workflows.

### Beehive

Beehives segregate Bees resources. For a multi users experience it is 
important to not compete on the same resources and prevent an user to 
consume all resources while others get their jobs pending.

Bees are organized in Beehives. A Bee can be part of multiple Beehives. 
A workflow have to be attached to a single Beehive.
A workflow cannot be executed on a Bee which is not part of that Beehive.

### Workflow

A workflow combines multiple jobs. It exists different job 
types. The simpliest type is a task. Then to glue tasks together there 
is parallel and sequence jobs.

You can take the following actions on a workflow:

* **Execute**: Start a workflow execution.
* **Cancel**: Cancel a workflow and all dependend tasks.
* **Recover**: Recover a workflow after it has been cancel or feld in 
error. The recover won't re-run completed jobs but only the non completed.
* **Delete**: Delete a workflow and jobs from the history and delete 
stored data from the disk all over Bees

#### Simple task job

Simple task job runs a task, aka process. The job takes for parmaters a
command with arguments and optionally how many CPUs it will consume.
The job is considered completed when the process exits with a 0 code. 
Otherwise it ends in error.

#### Parallel jobs

Parallel job runs children jobs in parallel. The job is considered 
completed when all children are completed. If there is any child 
in error, the job fall in error, but other chlidren jobs continue 
running unless the user ask for cancel.

#### Sequential jobs

Sequence job runs children jobs in sequence. A child job cannot start 
until the previous chlid is not completed. The job is considered 
completed when all children are completed. If there is any child in 
error, the job stops in error. I it remains non started children jobs,
they won't start.

## Bees

A Bee is a services or a daemon deployed on a machine. Once it is
enrolled into a Colony, the machine's resources are scanned and 
made available for that Colony.

The main resource scanned is the number of CPUs. It will condition
the Bee capacity for its Colony. The disk space and the memory are
also monitored. Those last two metrics help the user to choose or not 
a Bee for a given task, which could consumes too much memory or disk 
than available.

If a task, executed by a Bee, requires a disk space storage, an 
isolated folder is created for each. The folder name is the task 
identifier known by the Bee itself. This folder is also the root
folder on which your process starts in. You can retrieved the full 
folder path from the task (aka process) with the environment 
variable `BEE_TASK_FOLDER`.

A Bee provides a Web API to report the state and progress of a 
running task. The end point is named Flower ([more details here](#flower)).
The API provides as well others endpoints about the Bee state and resources.
But those endpoints are mostly used by the Colony on which the Bee
works for.

## Flower

The Flower is used by the running task on a remote Bee.
It's a simple interface to interact with the Bee supervisor to report the
state and progress of the running task.

All the information about the Bee and the running task are provided
through environment variables.

Environment variables:

* `BEE_PORT`: The port used by the Bee API. The client is always ran
locally, so the localhost is used for the URL.
* `BEE_TASK_ID`: The task id given to the running task by the Bee. 
This identifier is used to identify the task within its Bee. It has 
to be provided when the state is updated.
* `BEE_TASK_FOLDER`: The task folder is an isolated folder dedicated 
to the task. It will remain on the disk untill the task is deleted. 
The process is started in that folder. So the relative path `.` is 
equivalent.
equivalent.

