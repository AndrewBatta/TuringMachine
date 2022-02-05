using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TuringMachine
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IHostApplicationLifetime _lifetime;
        private Dictionary<string, Action<Stack>> _actions;

        public Worker(ILogger<Worker> logger, IHostApplicationLifetime lifetime)
        {
            _logger = logger;
            _lifetime = lifetime;
            InitializeActions();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                string testString = "5 6 10 DUP POP - 10 123 +";
                Stack stack = new();

                await Task.Run(() =>
                {
                    string[] instructions = testString.Split(' ');

                    foreach (string instruction in instructions)
                    {
                        if (stoppingToken.IsCancellationRequested)
                            return;

                        if (_actions.ContainsKey(instruction))
                        {
                            _actions[instruction].Invoke(stack);
                            continue;
                        }

                        stack.Push(int.Parse(instruction));
                    }

                }, stoppingToken);


                CheckStackCount(ref stack);
                _logger.LogInformation($"Final value: {stack.Pop()}");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown from machine!!!");
            }
            finally
            {
                _lifetime.StopApplication();
            }
        }

        private void CheckStackCount(ref Stack stack)
        {
            if (stack.Count == 0)
                throw new Exception("Stack is empty");
        }

        private void POP(ref Stack stack)
        {
            CheckStackCount(ref stack);
            stack.Pop();
        }

        private void DUP(ref Stack stack)
        {
            CheckStackCount(ref stack);

            int value = (int)stack.Pop();
            stack.Push(Validate(value * 2));
        }

        private void Add(ref Stack stack)
        {
            if (stack.Count < 2)
                throw new Exception("Stack does not have enough data for addition operation");

            int firstValue = (int)stack.Pop();
            int secondValue = (int)stack.Pop();

            stack.Push(Validate(firstValue + secondValue));
        }

        private void Sub(ref Stack stack)
        {
            if (stack.Count < 2)
                throw new Exception("Stack does not have enough data for subtraction operation");

            int firstValue = (int)stack.Pop();
            int secondValue = (int)stack.Pop();

            stack.Push(Validate(firstValue - secondValue));
        }

        private int Validate(int value)
        {
            if (value >= 1048576 || value < 0)
                throw new ArgumentOutOfRangeException($"Value {value} is out of range");

            return value;
        }

        private void InitializeActions()
        {
            _actions = new Dictionary<string, Action<Stack>>()
            {
                { "+", (stack) => Add(ref stack) },
                { "-", (stack) => Sub(ref stack) },
                { "DUP", (stack) => DUP(ref stack) },
                { "POP", (stack) => POP(ref stack) }
            };
        }
    }
}
