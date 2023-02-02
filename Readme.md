# Honey

Honey is a distributed platform which allows you to execute multiple 
tasks (aka processes) through multiple Bees (aka machines).

Honey allows you to parallelize and sequences your tasks around 
jobs to create execution workflows on a fully distributed platform.

Honey provides a web API to the end user as well as a user friendly 
web application on which you can follow and monitor your workflow 
progresses and the state of your distributed platform.

Honey is a multi users platform. In order to sgregate the platform
resources between users, each task is attached to a queue. A queue 
can be restrcted to a subset of Bees.

## Bees

A Bee is a services or a daemon deployed on a machine. Once it is
enrolled into a beehive, the machine's resources are scanned and 
made available for the Beehive.

The main resources scanned are the number of CPUs. They will condition
the Bee capacity for its Beehive. The disk space and the memory are
also monitored. Those last two metrics help the user to choose or not 
a Bee for a given task, which could consumes too much memory or disk 
than available.

If a task, executed by a Bee, requires a disk space storage, an 
isolated folder is created for it. The folder name is the task 
identifier known by the Bee itself. This folder is also the root
folder on which your process starts in. You can retrieved this 
folder path from the task (aka process) with the environment 
variable `BEE_TASK_FOLDER`.

A Bee provides a Web API to report the state and progress of a 
running task. The end point is named Flower ([more details here](#flower)).
The API provides as well others endpoints about the Bee state and resources.
But those endpoints are mostly used by the Beehive on which the Bee
has been enrolled.

## Flower

The Flower is used by the distributed running task on a remote Bee.
It's a simple interface to interact with the Bee host to report the
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

## Beehive

### Queue

### Colony

#### Workflow

#### Simple task job
#### Parallel jobs
#### Sequential jobs