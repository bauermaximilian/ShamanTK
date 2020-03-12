/* 
 * Eterra Framework
 * A simple framework for creating multimedia applications.
 * Copyright (C) 2020, Maximilian Bauer (contact@lengo.cc)
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Eterra
{
    /// <summary>
    /// Provides a log as static class, which is used by the components of
    /// <see cref="Eterra"/>.
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Defines the possible levels of a message, which describe how 
        /// important a specific message to the program execution or the user.
        /// </summary>
        public enum MessageLevel { Trace, Information, Warning, Error }

        /// <summary>
        /// A message log event, which holds informations about a certain event
        /// which occurred during framework operations.
        /// </summary>
        public class EventArgs : System.EventArgs
        {
            private static Regex wordWrapRegex = new Regex("[^\\s]+");
            private static Regex lineBreakRegex = new Regex(".+");

            /// <summary>
            /// Gets the level of the log message.
            /// </summary>
            public MessageLevel Level { get; }

            /// <summary>
            /// Gets the message of the log event. Must not be null.
            /// </summary>
            /// <exception cref="ArgumentNullException">
            /// Is thrown when the property is attempted to be set to null.
            /// </exception>
            public string Message { get; }

            /// <summary>
            /// Gets the name of the source object, which initiated the
            /// creation of this <see cref="EventArgs"/> instance, or null.
            /// </summary>
            public string Source { get; }

            /// <summary>
            /// Gets the time the log event occurred.
            /// </summary>
            public DateTime Time { get; }

            /// <summary>
            /// Creates a new instance of the <see cref="EventArgs"/> class.
            /// </summary>
            /// <param name="level">The level of the log event.</param>
            /// <param name="message">The message of the log event.</param>
            /// <param name="source">
            /// The name of the source object, which issued this log message.
            /// Can be null.
            /// </param>
            /// <exception cref="ArgumentNullException">
            /// Is thrown when <paramref name="message"/> is null.
            /// </exception>
            /// <exception cref="ArgumentException">
            /// Is thrown when <paramref name="level"/> is invalid.
            /// </exception>
            public EventArgs(MessageLevel level, string message,
                string source)
            {
                Time = DateTime.Now;
                if (!Enum.IsDefined(typeof(MessageLevel), level))
                    throw new ArgumentException("The specified message " +
                        "level is invalid.");
                Level = level;
                Message = message ??
                    throw new ArgumentNullException(nameof(message));
                Source = source;
            }

            /// <summary>
            /// Formats the current log message to a string.
            /// </summary>
            /// <returns>The current log event as string.</returns>
            public override string ToString()
            {
                return ToString(int.MaxValue);
            }

            /// <summary>
            /// Formats the current log message as a string and adds line
            /// breaks and spaces as indent after a specified amount of 
            /// characters (only suitable for monospace fonts, e.g. in console 
            /// output).
            /// </summary>
            /// <param name="lineWidth">
            /// The maximum line width in characters. If the value is negative 
            /// or too small for any message to reasonably fit in one line, 
            /// the value is ignored.
            /// </param>
            /// <returns>The current log event as string.</returns>
            public string ToString(int lineWidth)
            {
                StringBuilder builder = new StringBuilder();

                switch (Level)
                {
                    case MessageLevel.Trace:
                        builder.Append("TRC"); break;
                    case MessageLevel.Information:
                        builder.Append("INF"); break;
                    case MessageLevel.Warning:
                        builder.Append("WRN"); break;
                    case MessageLevel.Error:
                        builder.Append("ERR"); break;
                }

                builder.Append('-');

                builder.Append(Time.Hour.ToString("D2"));
                builder.Append(':');
                builder.Append(Time.Minute.ToString("D2"));
                builder.Append(':');
                builder.Append(Time.Second.ToString("D2"));

                if (!string.IsNullOrWhiteSpace(Source))
                {
                    builder.Append(" [");
                    builder.Append(Source);
                    builder.Append(']');
                }

                int minLineWidth = builder.ToString().Length;
                if ((minLineWidth + Environment.NewLine.Length) >= lineWidth)
                    minLineWidth = int.MinValue;

                int caret = minLineWidth;

                Match linePart = lineBreakRegex.Match(Message);
                do
                {
                    Match wordPart = wordWrapRegex.Match(linePart.Value);
                    do
                    {
                        if ((caret + wordPart.Length
                            + Environment.NewLine.Length + 1) > lineWidth
                            && caret > minLineWidth)
                        {
                            builder.AppendLine();
                            for (int i = 0; i <= minLineWidth; i++)
                                builder.Append(' ');
                            caret = minLineWidth + 1;
                        }
                        else
                        {
                            builder.Append(' ');
                            caret++;
                        }

                        builder.Append(wordPart.Value);
                        caret += wordPart.Length;
                        wordPart = wordPart.NextMatch();
                    } while (wordPart.Success);

                    linePart = linePart.NextMatch();

                    if (linePart.Success)
                    {
                        builder.AppendLine();
                        for (int i = 0; i < minLineWidth; i++)
                            builder.Append(' ');
                        caret = minLineWidth + 1;
                    }
                    else break;

                } while (true);

                return builder.ToString();
            }
        }

        private static readonly object logObject = new object();
        private const int exceptionMessageDepth = 10;

        /// <summary>
        /// Gets the highest <see cref="MessageLevel"/> which was used for
        /// a message in the current <see cref="Log"/>.
        /// </summary>
        public static MessageLevel HighestLogMessageLevel
        { get; private set; } = MessageLevel.Trace;

        /// <summary>
        /// Occurs after a new message was logged.
        /// </summary>
        public static event EventHandler<EventArgs> MessageLogged;

        /// <summary>
        /// Gets or sets a value indicating whether the messages from 
        /// exceptions logged with <see cref="Error(Exception, string)"/> 
        /// will be in english (<c>true</c>) or the language of the system 
        /// (<c>false</c>). Note that changing this property will change the
        /// value of the <see cref="Thread.CurrentUICulture"/> property of
        /// the <see cref="Thread.CurrentThread"/>.
        /// </summary>
        /// <remarks>
        /// Under certain circumstanges, some exception messages might still
        /// be displayed in the system culture - this is not a bug, but a 
        /// limitation of the .NET framework.
        /// </remarks>
        public static bool UseEnglishExceptionMessages
        {
            get => useEnglishExceptionMessages;
            set
            {
                if (value)
                {
                    previousUiCulture = Thread.CurrentThread.CurrentUICulture;
                    Thread.CurrentThread.CurrentUICulture =
                        CultureInfo.InvariantCulture;
                }
                else if (!value && useEnglishExceptionMessages != value)
                {
                    Thread.CurrentThread.CurrentUICulture =
                        previousUiCulture;
                }

                useEnglishExceptionMessages = value;
            }
        }
        private static bool useEnglishExceptionMessages;
        private static CultureInfo previousUiCulture;

        /// <summary>
        /// Gets or sets a value indicating whether any <see cref="Exception"/>
        /// logged with the <see cref="Error(string, Exception, string)"/>
        /// or the <see cref="Error(Exception, string)"/> method should include
        /// the name of the file and the line number where the exception
        /// occurred (<c>true</c>) or not (<c>false</c>, default).
        /// Only displays any information if the debug informations/PDB files 
        /// are available for the assembly the execution occurred in.
        /// </summary>
        public static bool IncludeLineNumbers { get; set; } = false;

        static Log()
        {
            useEnglishExceptionMessages = 
                Thread.CurrentThread.CurrentUICulture.IsNeutralCulture ||
                Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName 
                == "en";
            previousUiCulture = Thread.CurrentThread.CurrentUICulture;
        }

        /// <summary>
        /// Adds a new message with the <see cref="MessageLevel"/>
        /// <see cref="MessageLevel.Trace"/> to the log.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        /// <param name="source">
        /// The (short) name of the instance which issued the log message.
        /// Can be null.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown whe <paramref name="message"/> is null.
        /// </exception>
        public static void Trace(string message, string source = null)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            AddToLog(new EventArgs(MessageLevel.Trace,
                message, source));
        }

        /// <summary>
        /// Adds a new message with the <see cref="MessageLevel"/>
        /// <see cref="MessageLevel.Information"/> to the log.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        /// <param name="source">
        /// The (short) name of the instance which issued the log message.
        /// Can be null.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown whe <paramref name="message"/> is null.
        /// </exception>
        public static void Information(string message, string source = null)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            AddToLog(new EventArgs(MessageLevel.Information,
                message, source));
        }

        /// <summary>
        /// Adds a new message with the <see cref="MessageLevel"/>
        /// <see cref="MessageLevel.Warning"/> to the log.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        /// <param name="source">
        /// The (short) name of the instance which issued the log message.
        /// Can be null.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown whe <paramref name="message"/> is null.
        /// </exception>
        public static void Warning(string message, string source = null)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            AddToLog(new EventArgs(MessageLevel.Warning,
                message, source));
        }

        /// <summary>
        /// Adds a new message with the <see cref="MessageLevel"/>
        /// <see cref="MessageLevel.Error"/> to the log.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        /// <param name="source">
        /// The (short) name of the instance which issued the log message.
        /// Can be null.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown whe <paramref name="message"/> is null.
        /// </exception>
        public static void Error(string message, string source = null)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            AddToLog(new EventArgs(MessageLevel.Error,
                message, source));
        }

        /// <summary>
        /// Adds a new message with the <see cref="MessageLevel"/>
        /// <see cref="MessageLevel.Error"/> to the log.
        /// </summary>
        /// <param name="message">
        /// The message to be logged before the <paramref name="exception"/>.
        /// </param>
        /// <param name="exception">
        /// The exception to be logged after the <paramref name="exception"/>. 
        /// All inner exceptions will be included in the log.
        /// </param>
        /// <param name="source">
        /// The (short) name of the instance which issued the log message.
        /// Can be null.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown whe <paramref name="exception"/> is null.
        /// </exception>
        public static void Error(Exception exception, string source = null)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            StringBuilder messageBuilder = new StringBuilder();
            string exceptionTypeName = exception.GetType().Name;

            //Correct english grammar is importanter than speed, right?
            char c = exceptionTypeName[0].ToString().ToLowerInvariant()[0];
            if (c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u')
                messageBuilder.Append("An ");
            else messageBuilder.Append("A ");

            messageBuilder.Append(exceptionTypeName);
            messageBuilder.Append(" ocurred");

            if (string.IsNullOrWhiteSpace(exception.Message))
                messageBuilder.AppendLine(".");
            else messageBuilder.Append(": ");

            Error(messageBuilder.ToString(), exception, source);
        }

        /// <summary>
        /// Adds a new message with the <see cref="MessageLevel"/>
        /// <see cref="MessageLevel.Error"/> to the log.
        /// </summary>
        /// <param name="message">
        /// The message to be logged before the <paramref name="exception"/>.
        /// </param>
        /// <param name="exception">
        /// The exception to be logged after the <paramref name="exception"/>. 
        /// All inner exceptions will be included in the log.
        /// </param>
        /// <param name="source">
        /// The (short) name of the instance which issued the log message.
        /// Can be null.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown whe <paramref name="exception"/> or 
        /// <paramref name="message"/> is null.
        /// </exception>
        public static void Error(string message, Exception exception, 
            string source = null)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            StringBuilder messageBuilder = new StringBuilder();

            messageBuilder.AppendLine(message);

            ExceptionToString(exception, messageBuilder, 0);

            AddToLog(new EventArgs(MessageLevel.Error,
                messageBuilder.ToString(), source));
        }

        private static void ExceptionToString(Exception exception,
            StringBuilder stringBuilder, int recursionCounter)
        {
            if (recursionCounter > exceptionMessageDepth) return;
            if (exception == null) return;

            stringBuilder.Append("> Inner ");
            stringBuilder.Append(exception.GetType().Name);

            if (IncludeLineNumbers)
            {
                StackFrame frame = new StackTrace(exception, true).GetFrame(0);
                if (frame != null)
                {
                    string fileName = frame.GetFileName();
                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        fileName = Path.GetFileName(fileName);
                        int fileLineNumber = frame.GetFileLineNumber();

                        stringBuilder.Append(" [");
                        stringBuilder.Append(fileName);
                        stringBuilder.Append('#');
                        stringBuilder.Append(fileLineNumber);
                        stringBuilder.Append(']');
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(exception.Message))
                stringBuilder.AppendLine(".");
            else
            {
                stringBuilder.Append(": ");
                if (exception.InnerException == null)
                    stringBuilder.AppendLine(exception.Message);
                else stringBuilder.AppendLine(exception.Message);
            }            

            ExceptionToString(exception.InnerException, stringBuilder,
                recursionCounter++);
        }

        private static void AddToLog(EventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            if (args.Level > HighestLogMessageLevel)
                HighestLogMessageLevel = args.Level;
            MessageLogged?.Invoke(logObject, args);
        }
    }
}
