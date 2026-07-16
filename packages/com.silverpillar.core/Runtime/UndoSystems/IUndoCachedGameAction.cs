using System;
using UnityEngine;

namespace SilverPillar.Core
{
    public interface IUndoCommand
    {
        public bool SetGameObject(GameObject gameoObject);
        public GameObject GetGameObject();

        public void Execute();
        public void Undo();

        public IUndoCommand Clone();

        public bool CopyTo(IUndoCommand other);
    }

    public class CommandHistory
    {
        private readonly BoundedList<IUndoCommand> m_UndoHistory;
        private readonly BoundedList<IUndoCommand> m_RedoHistory;

        private IUndoCommand m_Command;

        private GameObject m_GameObject;

        private int m_Depth = 32;

        public CommandHistory(IUndoCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            m_Command = command.Clone();

            if (m_Command == null)
                throw new InvalidOperationException("Command clone returned null.");

            m_UndoHistory = new BoundedList<IUndoCommand>(m_Depth);
            m_RedoHistory = new BoundedList<IUndoCommand>(m_Depth);

            // Fill the internal reusable slots, but do not mark them as active history.
            m_UndoHistory.Fill(CreateHistoryCommandSlot, false);
            m_RedoHistory.Fill(CreateHistoryCommandSlot, false);
        }

        public bool SetGameObject(GameObject gameObject)
        {
            bool changedGameObject = m_GameObject != gameObject;

            m_GameObject = gameObject;

            if (changedGameObject)
            {
                ClearHistory();
            }

            bool result = true;

            result &= m_Command.SetGameObject(gameObject);
            result &= m_UndoHistory.ForEachSlot(command => command.SetGameObject(gameObject));
            result &= m_RedoHistory.ForEachSlot(command => command.SetGameObject(gameObject));

            return result;
        }

        public GameObject GetGameObject()
        {
            if (m_GameObject != null)
                return m_GameObject;

            if (m_Command != null)
                return m_Command.GetGameObject();

            return null;
        }

        public void Execute()
        {
            if (m_Command == null)
                return;

            m_Command.Execute();

            bool pushed = m_UndoHistory.PushCopyOf(m_Command, CopyCommandData);

            if (!pushed)
                return;

            // Once a new command is executed, the old redo branch is no longer valid.
            // This does not destroy the reusable command instances.
            m_RedoHistory.Clear();
        }

        public void Undo()
        {
            if (!m_UndoHistory.TryGetLast(out IUndoCommand command))
                return;

            command.Undo();

            bool pushed = m_RedoHistory.PushCopyOf(command, CopyCommandData);

            if (!pushed)
                return;

            m_UndoHistory.PopDiscard();
        }

        public void Redo()
        {
            if (!m_RedoHistory.TryGetLast(out IUndoCommand command))
                return;

            command.Execute();

            bool pushed = m_UndoHistory.PushCopyOf(command, CopyCommandData);

            if (!pushed)
                return;

            m_RedoHistory.PopDiscard();
        }

        public void SetDepth(int depth)
        {
            m_Depth = Mathf.Max(0, depth);

            m_UndoHistory.SetCapacity(
                m_Depth,
                CreateHistoryCommandSlot,
                CopyCommandData
            );

            m_RedoHistory.SetCapacity(
                m_Depth,
                CreateHistoryCommandSlot,
                CopyCommandData
            );
        }

        public int GetDepth()
        {
            return m_Depth;
        }

        public void ClearHistory()
        {
            // Do not liberate memory.
            // The command slots stay allocated and reusable.
            m_UndoHistory.Clear(false);
            m_RedoHistory.Clear(false);
        }

        private IUndoCommand CreateHistoryCommandSlot()
        {
            if (m_Command == null)
                throw new InvalidOperationException("Cannot create history slot because command is null.");

            IUndoCommand clone = m_Command.Clone();

            if (clone == null)
                throw new InvalidOperationException("Command clone returned null.");

            if (m_GameObject != null)
            {
                bool setResult = clone.SetGameObject(m_GameObject);

                if (!setResult)
                    throw new InvalidOperationException("Could not set GameObject on command history slot.");
            }

            return clone;
        }

        private bool CopyCommandData(IUndoCommand source, IUndoCommand target)
        {
            if (source == null)
                return false;

            if (target == null)
                return false;

            return source.CopyTo(target);
        }
    }
}