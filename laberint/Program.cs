using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Labirint
{
    internal class Program
    {
        static char wall = '#';
        static char path = ' ';
        static char player = 'P'; // Символ игрока
        static char goal = 'G'; // Символ цели
        static char hunter = 'H'; // Символ обычного охотника
        static char trail = '.'; // Символ следа игрока
        static Random random = new Random();
        static int playerX = 1; // Начальная позиция игрока
        static int playerY = 1; // Начальная позиция игрока
        static int hunterX = 1; // Начальная позиция обычного охотника
        static int hunterY = 1; // Начальная позиция обычного охотника
        static (int dx, int dy) lastDirection = (0, 0); // Последнее направление движения игрока
        static int goalX, goalY; // Позиция цели
        static int achievements = 0; // Счетчик достижений
        static int maxAchievements = 0; // Максимальное количество очков
        static int wallsBroken = 0; // Количество сломанных стен
        static int mazesCompleted = 0; // Количество пройденных лабиринтов
        static int mazesRegenerated = 0; // Количество сбросов лабиринта
        static char[,] maze; // Лабиринт
        static int width, height; // Размеры лабиринта
        static bool hunterActive = false; // Флаг активности обычного охотника
        static int hunterWaitTime = 0; // Время ожидания обычного охотника после спавна
        static int lastPlayerX = 1; // Последняя позиция игрока по X
        static int lastPlayerY = 1; // Последняя позиция игрока по Y
        static int hunterMoveCooldown = 0; // Счетчик для ограничения скорости обычного охотника
        static double hunterSpeed = 1.0; // Скорость обычного охотника (1.0 = стандартная скорость)

        static void Main(string[] args)
        {
            Console.SetWindowSize(100, 30);
            Console.SetBufferSize(100, 30);
            Console.ForegroundColor = ConsoleColor.Yellow;
            string asciiArt = @"
                                                    . ,-:;//;:=,
                                                 . :H@@@MM@M#H/.,+%;,
                                             ,/X+ +M@@M@MM%=,-%HMMM@X/,
                                            -+@MM; $M@@MH+-,;XMMMM@MM MM@+-
                                          ;@M@@M- XM@X;. -+XXXXXHHH@M@M#@/.
                                        ,%MM@@MH ,@%=              .---=-=:=,.
                                         =@#@@@MX.,                -%HX$%%%:;
                                       =-./@M@M$                   .;@MMMM@MM:
                                        X@/  -$MM/                    . +MM@@@M$
                                      ,@M@H: :@:                    . =X#@@@@-
                                       ,@@@MMX, .                     /H- ;@M@M=
                                      .H@@@@M@+,                    %MM+..%#$.
                                       /MMMM@MMH /.                  XM@MH; =;
                                         /%+%$XHH@$=              , .H@@@@MX,
                                         .=--------.           -%H.,@@@@@ MX,
                                          .%MM@@@HHHXX$%+- .:$MMX =M@@MM%.
                                           =XMMM@MM@MM#H;,-+HMM@M+ /MMMX=
                                              =%@M@M#@$-.=$@MM@ @@M; %M%=
                                               ,:+$+-,/H#MMMMMMM@= =,
                                                    =++%%%%+/:-.";
            PrintAsciiArt(asciiArt);
            Console.ResetColor();

            // Инструкция по кнопкам
            Console.Clear();
            Console.WriteLine("Инструкция по кнопкам:");
            Console.WriteLine("W - вверх");
            Console.WriteLine("A - влево");
            Console.WriteLine("S - вниз");
            Console.WriteLine("D - вправо");
            Console.WriteLine("Y - сломать стену");
            Console.WriteLine("R - перегенерировать лабиринт с учетом размера окна");
            Console.WriteLine("Q - выйти из игры");
            Console.WriteLine("\nНажмите любую клавишу для продолжения...");
            Console.ReadKey(true);

            width = 11; // Начальная ширина лабиринта
            height = 11; // Начальная высота лабиринта

            maze = GenerateMaze(width, height);
            SetGoal(maze);
            DisplayMaze(maze);

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Q) break; // Выход из игры
                    switch (key.Key)
                    {
                        case ConsoleKey.W:
                            MovePlayer(0, -1, ref maze, ref width, ref height);
                            break;
                        case ConsoleKey.A:
                            MovePlayer(-1, 0, ref maze, ref width, ref height);
                            break;
                        case ConsoleKey.S:
                            MovePlayer(0, 1, ref maze, ref width, ref height);
                            break;
                        case ConsoleKey.D:
                            MovePlayer(1, 0, ref maze, ref width, ref height);
                            break;
                        case ConsoleKey.Y:
                            if (achievements >= -10)
                            {
                                BreakWall(maze);
                                UpdateHunterSpeed(); // Обновляем скорость охотника
                                DisplayMaze(maze);
                            }
                            else
                            {
                                Console.WriteLine("Вы не можете копать стены при достижениях ниже -10.");
                            }
                            break;
                        case ConsoleKey.R:
                            RegenerateMazeWithWindowSize(ref maze, ref width, ref height); // Перегенерация лабиринта с учетом размера окна
                            SetGoal(maze); // Устанавливаем новую цель
                            DisplayMaze(maze);
                            break;
                    }
                }
                else
                {
                    if (hunterWaitTime > 0)
                    {
                        hunterWaitTime--;
                        if (hunterWaitTime == 0)
                        {
                            hunterActive = true;
                        }
                    }
                    else if (hunterActive)
                    {
                        MoveHunter(ref maze);
                    }

                    Thread.Sleep(50); // Небольшая задержка для уменьшения нагрузки
                }
            }

            EndGame();
        }

        static void PrintCentered(string text)
        {
            int consoleWidth = Console.WindowWidth;
            int spacesToPad = (consoleWidth - text.Length) / 2;
            Console.WriteLine(new string(' ', spacesToPad) + text);
        }

        static void PrintAsciiArt(string art)
        {
            var lines = art.Split(new[] { '\n' }, StringSplitOptions.None);
            int linesToPrint = 3;
            for (int i = 0; i < lines.Length; i += linesToPrint)
            {
                for (int j = 0; j < linesToPrint && (i + j) < lines.Length; j++)
                {
                    Console.WriteLine(lines[i + j]);
                }
                int delay = random.Next(100, 501);
                Thread.Sleep(delay);
            }
            for (int i = lines.Length - (lines.Length % linesToPrint); i < lines.Length; i++)
            {
                Console.WriteLine(lines[i]);
            }
            Thread.Sleep(50);
        }

        static char[,] GenerateMaze(int width, int height)
        {
            char[,] maze = new char[height, width];
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    maze[i, j] = wall;
                }
            }
            RandomizeMaze(maze, 1, 1);
            return maze;
        }

        static void RandomizeMaze(char[,] maze, int x, int y)
        {
            maze[y, x] = path;
            List<(int, int)> directions = new List<(int, int)> { (0, 2), (2, 0), (0, -2), (-2, 0) };
            directions = Shuffle(directions);

            foreach (var (dx, dy) in directions)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (nx > 0 && nx < maze.GetLength(1) - 1 && ny > 0 && ny < maze.GetLength(0) - 1 && maze[ny, nx] == wall)
                {
                    maze[y + dy / 2, x + dx / 2] = path;
                    RandomizeMaze(maze, nx, ny);
                }
            }
        }

        static List<T> Shuffle<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            return list;
        }

        static void SetGoal(char[,] maze)
        {
            do
            {
                goalX = random.Next(1, maze.GetLength(1) - 1);
                goalY = random.Next(1, maze.GetLength(0) - 1);
            } while (maze[goalY, goalX] != path);
            maze[goalY, goalX] = goal;
        }

        static void DisplayMaze(char[,] maze)
        {
            Console.Clear();
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < maze.GetLength(0); i++)
            {
                for (int j = 0; j < maze.GetLength(1); j++)
                {
                    if (i == playerY && j == playerX)
                    {
                        sb.Append("\u001b[32mP\u001b[0m"); // Зеленый цвет для игрока
                    }
                    else if (i == hunterY && j == hunterX && hunterActive)
                    {
                        sb.Append("\u001b[31mH\u001b[0m"); // Красный цвет для обычного охотника
                    }
                    else if (maze[i, j] == wall)
                    {
                        sb.Append("\u001b[90m#\u001b[0m"); // Серый цвет для стен
                    }
                    else if (maze[i, j] == path)
                    {
                        sb.Append(" "); // Черный цвет для пути (по умолчанию)
                    }
                    else if (maze[i, j] == goal)
                    {
                        sb.Append("\u001b[31mG\u001b[0m"); // Красный цвет для цели
                    }
                    else if (maze[i, j] == trail)
                    {
                        sb.Append("."); // Символ следа игрока
                    }
                    else
                    {
                        sb.Append(" ");
                    }
                }
                sb.AppendLine();
            }

            // Индикатор времени ожидания охотника
            if (hunterWaitTime > 0)
            {
                int waitCubes = 3 - (hunterWaitTime / 20); // 3 секунды * 20 циклов в секунду (50 мс задержка)
                sb.AppendLine($"Достижения: {achievements} | Охотник активируется через: [{new string('\u2588', waitCubes)}{new string(' ', 3 - waitCubes)}]");
            }
            else
            {
                sb.AppendLine($"Достижения: {achievements}");
            }

            Console.Write(sb.ToString());
        }

        static void MovePlayer(int dx, int dy, ref char[,] maze, ref int width, ref int height)
        {
            int newX = playerX + dx;
            int newY = playerY + dy;

            if (newX >= 0 && newX < maze.GetLength(1) && newY >= 0 && newY < maze.GetLength(0))
            {
                if (maze[newY, newX] == path || maze[newY, newX] == goal || maze[newY, newX] == trail)
                {
                    if (maze[newY, newX] == goal)
                    {
                        achievements++;
                        maxAchievements = Math.Max(maxAchievements, achievements);
                        mazesCompleted++;
                        Console.WriteLine("Вы достигли цели! Достижения: " + achievements);

                        maze[playerY, playerX] = trail; // Оставляем след на месте цели
                        RegenerateMaze(ref maze, ref width, ref height); // Перегенерация лабиринта
                        SetGoal(maze); // Устанавливаем новую цель

                        // Обновляем скорость охотника
                        UpdateHunterSpeed();
                    }

                    lastDirection = (dx, dy);
                    playerX = newX;
                    playerY = newY;
                    maze[playerY, playerX] = trail; // Оставляем след игрока

                    // Активируем охотника после достижения 5 очков
                    if (achievements >= 5 && !hunterActive && hunterWaitTime == 0)
                    {
                        ActivateHunter(ref maze);
                    }
                }
            }

            DisplayMaze(maze);
        }

        static void ActivateHunter(ref char[,] maze)
        {
            if (!hunterActive && hunterWaitTime == 0)
            {
                // Находим первую точку следа (trail)
                for (int i = 0; i < maze.GetLength(0); i++)
                {
                    for (int j = 0; j < maze.GetLength(1); j++)
                    {
                        if (maze[i, j] == trail)
                        {
                            hunterX = j;
                            hunterY = i;
                            hunterWaitTime = 60; // 3 секунды * 20 циклов в секунду (50 мс задержка)
                            return;
                        }
                    }
                }

                // Если следов нет, охотник появляется на последней позиции игрока
                hunterX = lastPlayerX;
                hunterY = lastPlayerY;
                hunterWaitTime = 60; // 3 секунды * 20 циклов в секунду (50 мс задержка)
            }
        }

        static void MoveHunter(ref char[,] maze)
        {
            if (!hunterActive) return;

            if (hunterMoveCooldown > 0)
            {
                hunterMoveCooldown--;
                return; // Охотник ждет, пока счетчик не достигнет нуля
            }

            // Найдем ближайший след игрока
            (int, int)? closestTrail = null;
            int minDistance = int.MaxValue;

            for (int i = 0; i < maze.GetLength(0); i++)
            {
                for (int j = 0; j < maze.GetLength(1); j++)
                {
                    if (maze[i, j] == trail)
                    {
                        int distance = Math.Abs(hunterX - j) + Math.Abs(hunterY - i);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestTrail = (j, i);
                        }
                    }
                }
            }

            if (closestTrail.HasValue)
            {
                int targetX = closestTrail.Value.Item1;
                int targetY = closestTrail.Value.Item2;

                // Определяем направление движения охотника
                int dx = Math.Sign(targetX - hunterX);
                int dy = Math.Sign(targetY - hunterY);

                int newX = hunterX + dx;
                int newY = hunterY + dy;

                if (newX >= 0 && newX < maze.GetLength(1) && newY >= 0 && newY < maze.GetLength(0))
                {
                    if (maze[newY, newX] == path || maze[newY, newX] == trail)
                    {
                        if (maze[newY, newX] == trail)
                        {
                            maze[newY, newX] = path; // Стираем след игрока
                        }
                        hunterX = newX;
                        hunterY = newY;

                        // Проверяем, догнал ли охотник игрока
                        if (newX == playerX && newY == playerY)
                        {
                            achievements -= 5;
                            Console.WriteLine("Охотник догнал вас! Достижения: " + achievements);

                            // Перегенерация лабиринта
                            RegenerateMaze(ref maze, ref width, ref height);
                            SetGoal(maze);
                            DisplayMaze(maze);

                            // Активируем охотника после достижения 5 очков
                            if (achievements >= 5 && !hunterActive && hunterWaitTime == 0)
                            {
                                ActivateHunter(ref maze);
                            }

                            // Проверяем, достиг ли игрок -10 очков
                            if (achievements <= -10)
                            {
                                EndGame();
                            }
                        }
                    }
                }
            }

            // Обновляем cooldown в зависимости от скорости охотника
            hunterMoveCooldown = Math.Max(1, (int)(5 / hunterSpeed)); // Минимальный cooldown = 1
        }

        static void RegenerateMaze(ref char[,] maze, ref int width, ref int height)
        {
            int maxConsoleWidth = Console.WindowWidth - 2; // Учитываем границы окна
            int maxConsoleHeight = Console.WindowHeight - 3; // Учитываем границы окна и строку для счета

            // Сохраняем последнюю позицию игрока
            lastPlayerX = playerX;
            lastPlayerY = playerY;

            // Увеличиваем размер лабиринта
            width = Math.Min(width + 2, maxConsoleWidth);
            height = Math.Min(height + 2, maxConsoleHeight);

            // Создаем новый лабиринт с новыми размерами
            maze = GenerateMaze(width, height);
            playerX = 1; // Возвращаем игрока к начальной позиции
            playerY = 1;

            // Охотник появляется на последней позиции игрока
            hunterX = lastPlayerX;
            hunterY = lastPlayerY;

            // Деактивируем охотника и сбрасываем время ожидания
            hunterActive = false;
            hunterWaitTime = 60; // 3 секунды * 20 циклов в секунду (50 мс задержка)

            // Сбрасываем скорость охотника и cooldown
            hunterSpeed = 1.0;
            hunterMoveCooldown = 0;

            mazesRegenerated++;
        }

        static void RegenerateMazeWithWindowSize(ref char[,] maze, ref int width, ref int height)
        {
            int maxConsoleWidth = Console.WindowWidth - 2; // Учитываем границы окна
            int maxConsoleHeight = Console.WindowHeight - 3; // Учитываем границы окна и строку для счета

            // Сохраняем последнюю позицию игрока
            lastPlayerX = playerX;
            lastPlayerY = playerY;

            // Устанавливаем размеры лабиринта в соответствии с размерами окна
            width = Math.Max(11, maxConsoleWidth); // Минимальная ширина 11
            height = Math.Max(11, maxConsoleHeight); // Минимальная высота 11

            // Создаем новый лабиринт с новыми размерами
            maze = GenerateMaze(width, height);
            playerX = 1; // Возвращаем игрока к начальной позиции
            playerY = 1;

            // Охотник появляется на последней позиции игрока
            hunterX = lastPlayerX;
            hunterY = lastPlayerY;

            // Деактивируем охотника и сбрасываем время ожидания
            hunterActive = false;
            hunterWaitTime = 60; // 3 секунды * 20 циклов в секунду (50 мс задержка)

            // Сбрасываем скорость охотника и cooldown
            hunterSpeed = 1.0;
            hunterMoveCooldown = 0;

            mazesRegenerated++;
        }

        static void BreakWall(char[,] maze)
        {
            List<(int, int)> possibleWalls = new List<(int, int)>
            {
                (playerX + lastDirection.dx, playerY + lastDirection.dy),
                (playerX - lastDirection.dx, playerY - lastDirection.dy),
                (playerX + lastDirection.dy, playerY - lastDirection.dx),
                (playerX - lastDirection.dy, playerY + lastDirection.dx)
            };

            foreach (var (x, y) in possibleWalls)
            {
                if (x > 0 && x < maze.GetLength(1) - 1 && y > 0 && y < maze.GetLength(0) - 1 && maze[y, x] == wall)
                {
                    maze[y, x] = path; // Ломаем стену
                    achievements--; // Уменьшаем достижения на 1
                    wallsBroken++;
                    Console.WriteLine("Стена сломана! Текущие достижения: " + achievements);

                    // Обновляем скорость охотника
                    UpdateHunterSpeed();
                    return;
                }
            }
            Console.WriteLine("Невозможно сломать стену здесь.");
        }

        static void UpdateHunterSpeed()
        {
            // Замедление при положительных достижениях
            if (achievements > 0)
            {
                hunterSpeed = Math.Max(0.2, 1.0 - (achievements / 10.0) * 0.2); // Минимальная скорость 0.2
            }

            // Ускорение при отрицательных достижениях
            if (achievements < 0)
            {
                hunterSpeed = Math.Min(2.0, 1.0 + (-achievements / 5.0) * 0.4); // Максимальная скорость 2.0
            }
        }

        static void EndGame()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Gray;
            PrintCentered("Игра окончена!");
            Console.WriteLine();
            PrintCentered($"Количество пройденных лабиринтов: {mazesCompleted}");
            PrintCentered($"Максимальное количество очков: {maxAchievements}");
            PrintCentered($"Количество сломанных стен: {wallsBroken}");
            PrintCentered($"Количество сбросов лабиринта: {mazesRegenerated}");
            PrintCentered($"Размер лабиринта: {width}x{height}");
            Console.ResetColor();
            Console.WriteLine();
            PrintCentered("Нажмите любую клавишу для выхода...");
            Console.ReadKey(true);
            Environment.Exit(0);
        }
    }
}