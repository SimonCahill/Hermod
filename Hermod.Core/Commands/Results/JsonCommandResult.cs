using System;

namespace Hermod.Core.Commands.Results {

    using Newtonsoft.Json;

    public class JsonCommandResult<T>: ICommandResult<T> {

		#region ICommandResult
        /// <inheritdoc/>
        public string? Message { get; set; }

        /// <inheritdoc/>
        public T Result { get; set; }

		object? ICommandResult.Result => throw new NotImplementedException();
		#endregion

		public JsonCommandResult(): this(string.Empty, Activator.CreateInstance<T>()) { }

        public JsonCommandResult(string message, T result) {
            Message = message;
            Result = result;
        }

        /// <summary>
        /// Converts the <see cref="Result"/> to a JSON-serialised string.
        /// </summary>
        /// <param name="settings">Settings for the JSON serialiser.</param>
        /// <returns>A string representation of <see cref="Result"/></returns>
        public string? ToJson(JsonSerializerSettings? settings) => JsonConvert.SerializeObject(Result, settings);

        /// <summary>
        /// Converts the <see cref="Result"/> to a JSON-serialised string.
        /// </summary>
        /// <param name="formatting">Formatting settings for the JSON serialiser.</param>
        /// <returns>A string representation of <see cref="Result"/></returns>
        public string? ToJson(Formatting formatting) => JsonConvert.SerializeObject(Result, formatting);
    }
}

