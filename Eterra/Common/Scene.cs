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
using System.Collections;
using System.Collections.Generic;

namespace Eterra.Common
{
    public class Scene : IEnumerable<Entity>, IDisposable
    {
        public class EntityCollectionUpdatedEventArgs : EventArgs
        {
            public bool WasAdded { get; }

            public bool WasRemoved => !WasAdded;

            public Entity Entity { get; }

            public EntityCollectionUpdatedEventArgs(Entity entity, 
                bool entityAdded)
            {
                Entity = entity ??
                    throw new ArgumentNullException(nameof(entity));
                WasAdded = entityAdded;
            }
        }

        private readonly List<Entity> entities = new List<Entity>();
        private readonly Dictionary<string, List<Entity>> namedEntities
            = new Dictionary<string, List<Entity>>();

        public int Count => entities.Count;

        public event EventHandler<EntityCollectionUpdatedEventArgs>
            EntityRemoved;

        public event EventHandler<EntityCollectionUpdatedEventArgs>
            EntityAdded;

        public Scene() { }

        public bool TryGet(string name, out Entity[] entities)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (namedEntities.TryGetValue(name,
                out List<Entity> entitySelection))
            {
                entities = entitySelection.ToArray();
                return true;
            }
            else
            {
                entities = new Entity[0];
                return false;
            }
        }

        public bool TryGetFirst(string name, out Entity entity)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (namedEntities.TryGetValue(name,
                out List<Entity> entitySelection))
            {
                entity = entitySelection[0];
                return true;
            }
            else
            {
                entity = null;
                return false;
            }
        }

        public Entity[] Get(string name)
        {
            if (TryGet(name, out Entity[] entities))
                return entities;
            else throw new ArgumentException("No entities with the " +
                "specified name exist in the current scene.");
        }

        public Entity GetFirst(string name)
        {
            if (TryGetFirst(name, out Entity entity))
                return entity;
            else throw new ArgumentException("No entity with the " +
                "specified name exists in the current scene.");
        }

        public Entity Add()
        {
            Entity item = new Entity(this);

            item.NameChanged += EntityNameChanged;
            item.LocationChanged += EntityLocationChanged;

            entities.Add(item);

            EntityAdded?.Invoke(this, new EntityCollectionUpdatedEventArgs(
                item, true));

            return item;
        }

        private void AddEntityToNamedCollection(Entity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (namedEntities.TryGetValue(entity.Name, 
                out List<Entity> entitySelection))
                entitySelection.Add(entity);
            else namedEntities.Add(entity.Name, new List<Entity>() { entity });
        }

        private void RemoveEntityFromNamedCollection(Entity entity,
            string nameInCollection = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            if (nameInCollection == null) nameInCollection = entity.Name;

            if (nameInCollection != null &&
                namedEntities.TryGetValue(nameInCollection,
                out List<Entity> entitySelection))
            {
                entitySelection.Remove(entity);
                if (entitySelection.Count == 0)
                    namedEntities.Remove(nameInCollection);
            }
        }

        private void EntityLocationChanged(object sender, EventArgs e) { }

        private void EntityNameChanged(object sender, 
            Entity.NameUpdatedEventArgs e)
        {
            if (e.PreviousName != null)
                RemoveEntityFromNamedCollection(e.Entity, e.PreviousName);
            if (e.Entity.Name != null)
                AddEntityToNamedCollection(e.Entity);
        }

        public void Clear()
        {
            while (entities.Count > 0) Remove(entities[0]);
        }

        public bool Contains(Entity item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            return entities.Contains(item);
        }

        public void CopyTo(Entity[] array, int arrayIndex)
        {
            try { entities.CopyTo(array, arrayIndex); }
            catch (ArgumentNullException) { throw; }
            catch (ArgumentOutOfRangeException) { throw; }
            catch (ArgumentException) { throw; }
        }

        public IEnumerator<Entity> GetEnumerator()
        {
            return entities.GetEnumerator();
        }

        public bool Remove(Entity item, bool disposeEntity = true)
        {
            try
            {
                if (Contains(item))
                {
                    item.NameChanged -= EntityNameChanged;
                    item.LocationChanged -= EntityLocationChanged;
                    RemoveEntityFromNamedCollection(item);
                    entities.Remove(item);
                    if (disposeEntity) item.Dispose();
                    EntityRemoved?.Invoke(this, 
                        new EntityCollectionUpdatedEventArgs(item, false));

                    return true;
                }
                else return false;
            }
            catch (ArgumentNullException) { throw; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            while (entities.Count > 0) Remove(entities[0], true);
        }
    }
}
