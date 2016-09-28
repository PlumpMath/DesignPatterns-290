﻿using DPA_Musicsheets.Model;
using Sanford.Multimedia.Midi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPA_Musicsheets
{
    public class ScoreBuilder
    {
        private static ScoreBuilder instance;
        private double currentDuration = 0;
        private int currentAbsoluteTicksNote = 0;

        public static ScoreBuilder Instance
        {
            get
            {
                if (instance == null)
                    instance = new ScoreBuilder();
                return instance;
            }
        }

        private ScoreBuilder()
        {
            
        }

        public Score BuildScoreFromMidi(String filePath)
        {
            Score score = new Score();
               
            // Read the MIDI sequence.
            var midiSequence = new Sequence();
            midiSequence.Load(filePath);

            int ticksPerBeat = midiSequence.Division;

            Tempo tempo = null;
            TimeSignature timeSignature = null;

            bool firstTimeSignature = true;

            //Create a new staff for each track in the sequence.
            for (int i = 0; i < midiSequence.Count; i++)
            {
                Staff staff = new Staff();
                staff.StaffNumber = i;

                Track track = midiSequence[i];

                Dictionary<int, Note> keyNoteMap = new Dictionary<int, Note>();

                foreach (var midiEvent in track.Iterator())
                {
                    switch (midiEvent.MidiMessage.MessageType)
                    {
                        // ChannelMessages zijn de inhoudelijke messages.
                        case MessageType.Channel:
                            var channelMessage = midiEvent.MidiMessage as ChannelMessage;

                            int keyCode = channelMessage.Data1;

                            // Note already exists, setNoteDuration
                            if (StaffSymbolFactory.Instance.ContainsNoteKey(keyCode) && (channelMessage.Data2 == 0 || channelMessage.Command == ChannelCommand.NoteOff))
                            {
                                double noteDuration = StaffSymbolFactory.Instance.SetNoteDuration(keyCode, midiEvent, ticksPerBeat, timeSignature);
                                //if(currentAbsoluteTicksNote != )
                                currentDuration += noteDuration;
                                //currentAbsoluteTicksNote
                                if (currentDuration >= 1) // temp very dirty solution
                                {
                                    staff.Symbols.Add(new Barline());
                                    currentDuration = 0;
                                }
                            }
                            // Create new Note
                            else if (channelMessage.Command == ChannelCommand.NoteOn && channelMessage.Data2 > 0)
                            {
                                StaffSymbol note = StaffSymbolFactory.Instance.ConstructNote(keyCode, midiEvent);
                                if (note != null)
                                {
                                    staff.Symbols.Add(note);
                                }
                                else
                                {
                                    Console.WriteLine("Error.");
                                }
                            }

                            break;
                        case MessageType.SystemExclusive:
                            break;
                        case MessageType.SystemCommon:
                            break;
                        case MessageType.SystemRealtime:
                            break;
                        case MessageType.Meta:
                            var metaMessage = midiEvent.MidiMessage as MetaMessage;
                            switch (metaMessage.MetaType)
                            {
                                case MetaType.TrackName:
                                    staff.StaffName = i + " " + Encoding.Default.GetString(metaMessage.GetBytes());
                                    break;
                                case MetaType.InstrumentName:
                                    staff.InstrumentName = Encoding.Default.GetString(metaMessage.GetBytes());
                                    break;
                                case MetaType.Tempo:
                                    tempo = (Tempo)StaffSymbolFactory.Instance.ConstructSymbol(metaMessage);
                                    staff.Symbols.Add(tempo);
                                    break;
                                case MetaType.TimeSignature:
                                    if (i == 0) // Control Track
                                    {
                                        if (firstTimeSignature)
                                        {
                                            timeSignature = (TimeSignature)StaffSymbolFactory.Instance.ConstructSymbol(metaMessage);
                                            staff.Symbols.Add(timeSignature);
                                            firstTimeSignature = false;
                                        }
                                        // else skip these frigging false timeSignatures disrupting time and space
                                    }
                                    else
                                    {
                                        timeSignature = (TimeSignature)StaffSymbolFactory.Instance.ConstructSymbol(metaMessage);
                                        staff.Symbols.Add(timeSignature);
                                        firstTimeSignature = false;
                                    }
                                    break;
                                default:
                                    staff.Symbols.Add(StaffSymbolFactory.Instance.ConstructSymbol(metaMessage));
                                    break;
                            }
                            break;
                    }
                }
                if (staff.StaffName == null)
                {
                    staff.StaffName = i.ToString();
                }
                score.Staves.Add(staff);
            }

            return score;
        }

        public Score BuildScoreFromLilyPond(String filePath)
        {
            Score score = new Score();
            LilyPondStaffAdapter staff = new LilyPondStaffAdapter();

            string fileText = File.ReadAllText(filePath);
            string[] tokens = fileText.Split(new string[] { " ", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            // Default relative note is c.
            string relativeNote = "c";

            for (int i = 0; i < tokens.Length; i++)
            {
                // Ignore empty lines.
                //if (tokens[i] == "")
                //    continue;
                if (tokens[i].StartsWith("\\"))
                {
                    string key = tokens[i].Substring(1);
                    switch (key)
                    {
                        case "relative":
                            string relativeNoteValue = tokens[++i];
                            relativeNote = relativeNoteValue;
                            break;
                        case "clef":
                            string cleffValue = tokens[++i];
                            staff.AddClef(cleffValue);
                            break;
                        case "time":
                            string timeValue = tokens[++i];
                            staff.AddTimeSignature(timeValue);
                            break;
                        case "tempo":
                            string tempoValue = tokens[++i];
                            staff.AddTempo(tempoValue);
                            break;
                        case "repeat":
                            string repeatType = tokens[++i];
                            switch (repeatType)
                            {
                                case "volta":
                                    int repeatCount = int.Parse(tokens[++i]);
                                    staff.Symbols.Add(new Repeat { Type = RepeatType.FORWARD });
                                    break;
                            }
                            break;
                        case "alternative":
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    if (tokens[i] == "|")
                        staff.Symbols.Add(new Barline());
                }
            }

            score.Staves.Add(staff);
            return score;
        }
    }
}
