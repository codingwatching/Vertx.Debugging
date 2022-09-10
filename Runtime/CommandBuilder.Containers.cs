using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vertx.Debugging
{
	public sealed partial class CommandBuilder
	{
		private class ListWrapper<T> : IDisposable where T : unmanaged
		{
			private const int InitialListCapacity = 32;

			public NativeList<T> List;
			public int Count => List.IsCreated ? List.Length : 0;

			public void Create() => List = new NativeList<T>(InitialListCapacity, Allocator.Persistent);

			public virtual void Dispose()
			{
				if (List.IsCreated)
					List.Dispose();
			}
		}

		private class ListAndBuffer<T> : ListWrapper<T> where T : unmanaged
		{
			private readonly int _bufferId;
			private GraphicsBuffer _buffer;

			private ListAndBuffer() { }

			public ListAndBuffer(string bufferName) => _bufferId = Shader.PropertyToID(bufferName);

			public void SetBufferData(CommandBuffer commandBuffer)
			{
				if (_buffer == null || _buffer.count < List.Capacity)
				{
					// Expand graphics buffer to encompass the capacity of the list.
					_buffer?.Dispose();
					_buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, List.Capacity, UnsafeUtility.SizeOf<T>());
				}

				commandBuffer.SetBufferData(_buffer, List.AsArray(), 0, 0, List.Length);
			}

			public void SetBufferToPropertyBlock(MaterialPropertyBlock propertyBlock) => propertyBlock.SetBuffer(_bufferId, _buffer);

			public override void Dispose()
			{
				base.Dispose();
				_buffer?.Dispose();
				_buffer = null;
			}
		}

		private class ShapeBuffersWithData<T> : IDisposable where T : unmanaged
		{
			private bool _dirty = true;
			private readonly ListWrapper<float> _durations = new ListWrapper<float>();
			private readonly ListAndBuffer<T> _elements;
			private readonly ListAndBuffer<Color> _colors = new ListAndBuffer<Color>("color_buffer");
			private readonly ListAndBuffer<Shapes.DrawModifications> _modifications = new ListAndBuffer<Shapes.DrawModifications>("modifications_buffer");

			private MaterialPropertyBlock _propertyBlock;

			public MaterialPropertyBlock PropertyBlock
			{
				get
				{
					if (_propertyBlock == null)
						_propertyBlock = new MaterialPropertyBlock();
					return _propertyBlock;
				}
			}

			public int Count => _elements.Count;

			public NativeList<T> InternalList => _elements.List;
			public NativeList<float> DurationsInternalList => _durations.List;
			public NativeList<Shapes.DrawModifications> ModificationsInternalList => _modifications.List;
			public NativeList<Color> ColorsInternalList => _colors.List;

			private ShapeBuffersWithData() { }

			public ShapeBuffersWithData(string bufferName) => _elements = new ListAndBuffer<T>(bufferName);

			public void Set(CommandBuffer commandBuffer, MaterialPropertyBlock propertyBlock)
			{
				if (_dirty)
				{
					_elements.SetBufferData(commandBuffer);
					_colors.SetBufferData(commandBuffer);
					_modifications.SetBufferData(commandBuffer);
					_dirty = false;
				}

				_elements.SetBufferToPropertyBlock(propertyBlock);
				_colors.SetBufferToPropertyBlock(propertyBlock);
				_modifications.SetBufferToPropertyBlock(propertyBlock);
			}

			public void SetDirty() => _dirty = true;

			private void EnsureCreated()
			{
				if (_elements.List.IsCreated)
					return;
				_elements.Create();
				_colors.Create();
				_modifications.Create();
				_durations.Create();
				_dirty = true;
			}

			public void Add(T shape, Color color, Shapes.DrawModifications modifications, float duration)
			{
				EnsureCreated();
				_elements.List.Add(shape);
				_colors.List.Add(color);
				_modifications.List.Add(modifications);
				_durations.List.Add(duration);
				_dirty = true;
			}

			public void Clear()
			{
				if (_elements.Count == 0)
					return;
				_elements.List.Clear();
				_colors.List.Clear();
				_modifications.List.Clear();
				_durations.List.Clear();
				_dirty = true;
			}

			public void Dispose()
			{
				if (!_elements.List.IsCreated)
					return;
				_elements.Dispose();
				_colors.Dispose();
				_modifications.Dispose();
				_durations.Dispose();
			}
		}
	}
}