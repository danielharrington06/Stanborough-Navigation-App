# Stanborough-Navigation-App

This is a project where I designed a navigation app between classrooms and important locations in my school. The target audience would be new students, students that struggle to remember their way around school and new and supply teachers. Because the overall UNity file was so large, I am only using version control on the scripts folder. The files here are Unity scripts attached to game objects.

The project essentially involves querying a database for data that is used to build a time matrix representing times between locations in the school. Then, I have a long algorithm combining Dijkstra's with other methods to pathfinding between rooms and/or nodes in my school. It then displays the Dijkstra path graphically.

This is the follow up to a previous repository in which I wrote out the backend algorithms used to build matrices, interface with the database and carry out pathfinding.
This code from there has been copied into this project and has been split up into different scripts and game objects.
