using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Palmtree;
using Palmtree.IO;

namespace ConsoleTasks
{
    public partial class ConsoleTask
    {
        private sealed class EnvironmentVariableModel
        {
            public EnvironmentVariableModel()
            {
                Name = "";
                Value = "";
            }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("value")]
            public string Value { get; set; }
        }

        private sealed class Model
        {
            static Model()
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            }

            public Model()
            {
                CommandFile = "";
                WorkingDirectory = "";
                ShellTypeText = "";
                PathEnvironmentVariables = [];
                EnvironmentVariables = [];
                RegisteredDateTime = "";
            }

            [JsonPropertyName("command_file")]
            public string CommandFile { get; set; }

            [JsonPropertyName("working_directory")]
            public string WorkingDirectory { get; set; }

            [JsonPropertyName("script_type")]
            public string ShellTypeText { get; set; }

            [JsonPropertyName("path_environment_variables")]
            public string[] PathEnvironmentVariables { get; set; }

            [JsonPropertyName("environment_variables")]
            public EnvironmentVariableModel[] EnvironmentVariables { get; set; }

            [JsonPropertyName("registered_datetime")]
            public string RegisteredDateTime { get; set; }

            public ShellType GetShellType()
            {
                if (string.Equals(ShellTypeText, "bat", StringComparison.OrdinalIgnoreCase))
                    return ShellType.CommandPrompt;
                if (string.Equals(ShellTypeText, "ps1", StringComparison.OrdinalIgnoreCase))
                    return ShellType.PowerShell;
                else
                    return ShellType.Unknown;
            }
        }

        [JsonSourceGenerationOptions(WriteIndented = true)]
        [JsonSerializable(typeof(Model))]
        [JsonSerializable(typeof(EnvironmentVariableModel))]
        private sealed partial class ModelSourceGenerator
            : JsonSerializerContext
        {
        }

        public ConsoleTask(string taskId, FilePath commandFile, DirectoryPath workingDirectory, IEnumerable<string> pathEnvironmentVariables, IEnumerable<(string Name, string Value)> environmentVariables, DateTime registeredDateTime)
            : this(taskId, ParseShellType(commandFile, nameof(commandFile)), commandFile, workingDirectory, pathEnvironmentVariables, environmentVariables, registeredDateTime)
        {
        }

        private ConsoleTask(string taskId, ShellType shellType, FilePath commandFile, DirectoryPath workingDirectory, IEnumerable<string> pathEnvironmentVariables, IEnumerable<(string Name, string Value)> environmentVariables, DateTime registeredDateTime)
        {
            TaskId = taskId;
            ShellType = shellType;
            CommandFile = commandFile ?? throw new ArgumentNullException(nameof(commandFile));
            WorkingDirectory = workingDirectory ?? throw new ArgumentNullException(nameof(workingDirectory));
            PathEnvironmentVariables = [.. pathEnvironmentVariables ?? throw new ArgumentNullException(nameof(pathEnvironmentVariables))];
            EnvironmentVariables = [.. environmentVariables ?? throw new ArgumentNullException(nameof(environmentVariables))];
            RegisteredDateTime = registeredDateTime;
        }

        public string TaskId { get; }
        public ShellType ShellType { get; }
        public FilePath CommandFile { get; }
        public DirectoryPath WorkingDirectory { get; }
        public IEnumerable<string> PathEnvironmentVariables { get; }
        public IEnumerable<(string Name, string Value)> EnvironmentVariables { get; }
        public DateTime RegisteredDateTime { get; }
        public bool IsRunning
        {
            get
            {
                Validation.Assert(!string.IsNullOrEmpty(TaskId));

                return TaskLockObject.IsLocked(TaskId);
            }
        }

        public DateTime? RunningStartDateTime
        {
            get
            {
                Validation.Assert(!string.IsNullOrEmpty(TaskId));

                var additionalInfo = ConsoleTaskQueue.GetAdditionalTaskInfo(TaskId);
                if (additionalInfo is null)
                    return null;
                return additionalInfo.Value.RunningStartDateTime;

            }
        }

        public string Serialize()
            => JsonSerializer.Serialize(
                new Model
                {
                    CommandFile = CommandFile.FullName,
                    WorkingDirectory = WorkingDirectory.FullName,
                    ShellTypeText = ShellType.ToInternalName(),
                    PathEnvironmentVariables = [.. PathEnvironmentVariables],
                    EnvironmentVariables = [.. EnvironmentVariables.Select(item => new EnvironmentVariableModel { Name = item.Name, Value = item.Value })],
                    RegisteredDateTime = RegisteredDateTime.ToString("O"),
                },
                typeof(Model),
                ModelSourceGenerator.Default);

        public static ConsoleTask? Deserialize(FilePath taskFile, string JsonText)
        {
            var model = JsonSerializer.Deserialize(JsonText, ModelSourceGenerator.Default.Model);
            if (model is null)
                return null;
            if (!DateTime.TryParse(model.RegisteredDateTime, out var registeredDateTime))
                registeredDateTime = Constants.NotAvailableDateTime;
            return
                new ConsoleTask(
                    taskFile.NameWithoutExtension.ToUpperInvariant(),
                    model.GetShellType(),
                    new FilePath(model.CommandFile),
                    new DirectoryPath(model.WorkingDirectory),
                    model.PathEnvironmentVariables,
                    model.EnvironmentVariables.Select(item => (item.Name, item.Value)),
                    registeredDateTime);
        }

        private static ShellType ParseShellType(FilePath commandFile, string parameterName)
            => OperatingSystem.IsWindows() && string.Equals(commandFile.Extension, ".bat", StringComparison.OrdinalIgnoreCase)
                ? ShellType.CommandPrompt
                : string.Equals(commandFile.Extension, ".ps1", StringComparison.OrdinalIgnoreCase)
                ? ShellType.PowerShell
                : throw new ArgumentException($"The extension of \"{parameterName}\" is not supported.", nameof(parameterName));
    }
}
