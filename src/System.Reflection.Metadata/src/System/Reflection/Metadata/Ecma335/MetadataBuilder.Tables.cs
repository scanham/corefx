// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335
{
    partial class MetadataBuilder
    {
        private const byte MetadataFormatMajorVersion = 2;
        private const byte MetadataFormatMinorVersion = 0;
        
        // type system table rows:
        private struct AssemblyRefTableRow { public Version Version; public BlobHandle PublicKeyToken; public StringHandle Name; public StringHandle Culture; public uint Flags; public BlobHandle HashValue; }
        private struct ModuleRow { public ushort Generation; public StringHandle Name; public GuidHandle ModuleVersionId; public GuidHandle EncId; public GuidHandle EncBaseId; }
        private struct AssemblyRow { public uint HashAlgorithm; public Version Version; public ushort Flags; public BlobHandle AssemblyKey; public StringHandle AssemblyName; public StringHandle AssemblyCulture; }
        private struct ClassLayoutRow { public ushort PackingSize; public uint ClassSize; public int Parent; }
        private struct ConstantRow { public byte Type; public int Parent; public BlobHandle Value; }
        private struct CustomAttributeRow { public int Parent; public int Type; public BlobHandle Value; }
        private struct DeclSecurityRow { public ushort Action; public int Parent; public BlobHandle PermissionSet; }
        private struct EncLogRow { public int Token; public byte FuncCode; }
        private struct EncMapRow { public int Token; }
        private struct EventRow { public ushort EventFlags; public StringHandle Name; public int EventType; }
        private struct EventMapRow { public int Parent; public int EventList; }
        private struct ExportedTypeRow { public uint Flags; public int TypeDefId; public StringHandle TypeName; public StringHandle TypeNamespace; public int Implementation; }
        private struct FieldLayoutRow { public int Offset; public int Field; }
        private struct FieldMarshalRow { public int Parent; public BlobHandle NativeType; }
        private struct FieldRvaRow { public uint Offset; public int Field; }
        private struct FieldDefRow { public ushort Flags; public StringHandle Name; public BlobHandle Signature; }
        private struct FileTableRow { public uint Flags; public StringHandle FileName; public BlobHandle HashValue; }
        private struct GenericParamConstraintRow { public int Owner; public int Constraint; }
        private struct GenericParamRow { public ushort Number; public ushort Flags; public int Owner; public StringHandle Name; }
        private struct ImplMapRow { public ushort MappingFlags; public int MemberForwarded; public StringHandle ImportName; public int ImportScope; }
        private struct InterfaceImplRow { public int Class; public int Interface; }
        private struct ManifestResourceRow { public uint Offset; public uint Flags; public StringHandle Name; public int Implementation; }
        private struct MemberRefRow { public int Class; public StringHandle Name; public BlobHandle Signature; }
        private struct MethodImplRow { public int Class; public int MethodBody; public int MethodDecl; }
        private struct MethodSemanticsRow { public ushort Semantic; public int Method; public int Association; }
        private struct MethodSpecRow { public int Method; public BlobHandle Instantiation; }
        private struct MethodRow { public int BodyOffset; public ushort ImplFlags; public ushort Flags; public StringHandle Name; public BlobHandle Signature; public int ParamList; }
        private struct ModuleRefRow { public StringHandle Name; }
        private struct NestedClassRow { public int NestedClass; public int EnclosingClass; }
        private struct ParamRow { public ushort Flags; public ushort Sequence; public StringHandle Name; }
        private struct PropertyMapRow { public int Parent; public int PropertyList; }
        private struct PropertyRow { public ushort PropFlags; public StringHandle Name; public BlobHandle Type; }
        private struct TypeDefRow { public uint Flags; public StringHandle Name; public StringHandle Namespace; public int Extends; public int FieldList; public int MethodList; }
        private struct TypeRefRow { public int ResolutionScope; public StringHandle Name; public StringHandle Namespace; }
        private struct TypeSpecRow { public BlobHandle Signature; }
        private struct StandaloneSigRow { public BlobHandle Signature; }
       
        // debug table rows:
        private struct DocumentRow { public BlobHandle Name; public GuidHandle HashAlgorithm; public BlobHandle Hash; public GuidHandle Language; }
        private struct MethodDebugInformationRow { public int Document; public BlobHandle SequencePoints; }
        private struct LocalScopeRow { public int Method; public int ImportScope; public int VariableList; public int ConstantList; public int StartOffset; public int Length; }
        private struct LocalVariableRow { public ushort Attributes; public ushort Index; public StringHandle Name; } 
        private struct LocalConstantRow { public StringHandle Name; public BlobHandle Signature; }
        private struct ImportScopeRow { public int Parent; public BlobHandle Imports; }
        private struct StateMachineMethodRow { public int MoveNextMethod; public int KickoffMethod; }
        private struct CustomDebugInformationRow { public int Parent; public GuidHandle Kind; public BlobHandle Value; }

        // type system tables:
        private readonly List<ModuleRow> _moduleTable = new List<ModuleRow>(1);
        private readonly List<AssemblyRow> _assemblyTable = new List<AssemblyRow>(1);
        private readonly List<ClassLayoutRow> _classLayoutTable = new List<ClassLayoutRow>();

        private readonly List<ConstantRow> _constantTable = new List<ConstantRow>();
        private int _constantTableLastParent;
        private bool _constantTableNeedsSorting;

        private readonly List<CustomAttributeRow> _customAttributeTable = new List<CustomAttributeRow>();
        private int _customAttributeTableLastParent;
        private bool _customAttributeTableNeedsSorting;

        private readonly List<DeclSecurityRow> _declSecurityTable = new List<DeclSecurityRow>();
        private int _declSecurityTableLastParent;
        private bool _declSecurityTableNeedsSorting;

        private readonly List<EncLogRow> _encLogTable = new List<EncLogRow>();
        private readonly List<EncMapRow> _encMapTable = new List<EncMapRow>();
        private readonly List<EventRow> _eventTable = new List<EventRow>();
        private readonly List<EventMapRow> _eventMapTable = new List<EventMapRow>();        
        private readonly List<ExportedTypeRow> _exportedTypeTable = new List<ExportedTypeRow>();
        private readonly List<FieldLayoutRow> _fieldLayoutTable = new List<FieldLayoutRow>();

        private readonly List<FieldMarshalRow> _fieldMarshalTable = new List<FieldMarshalRow>();
        private int _fieldMarshalTableLastParent;
        private bool _fieldMarshalTableNeedsSorting;

        private readonly List<FieldRvaRow> _fieldRvaTable = new List<FieldRvaRow>();
        private readonly List<FieldDefRow> _fieldTable = new List<FieldDefRow>();
        private readonly List<FileTableRow> _fileTable = new List<FileTableRow>();
        private readonly List<GenericParamConstraintRow> _genericParamConstraintTable = new List<GenericParamConstraintRow>();
        private readonly List<GenericParamRow> _genericParamTable = new List<GenericParamRow>();
        private readonly List<ImplMapRow> _implMapTable = new List<ImplMapRow>();
        private readonly List<InterfaceImplRow> _interfaceImplTable = new List<InterfaceImplRow>();
        private readonly List<ManifestResourceRow> _manifestResourceTable = new List<ManifestResourceRow>();
        private readonly List<MemberRefRow> _memberRefTable = new List<MemberRefRow>();
        private readonly List<MethodImplRow> _methodImplTable = new List<MethodImplRow>();

        private readonly List<MethodSemanticsRow> _methodSemanticsTable = new List<MethodSemanticsRow>();
        private int _methodSemanticsTableLastAssociation;
        private bool _methodSemanticsTableNeedsSorting;

        private readonly List<MethodSpecRow> _methodSpecTable = new List<MethodSpecRow>();
        private readonly List<MethodRow> _methodDefTable = new List<MethodRow>();
        private readonly List<ModuleRefRow> _moduleRefTable = new List<ModuleRefRow>();
        private readonly List<NestedClassRow> _nestedClassTable = new List<NestedClassRow>();
        private readonly List<ParamRow> _paramTable = new List<ParamRow>();
        private readonly List<PropertyMapRow> _propertyMapTable = new List<PropertyMapRow>();
        private readonly List<PropertyRow> _propertyTable = new List<PropertyRow>();
        private readonly List<TypeDefRow> _typeDefTable = new List<TypeDefRow>();
        private readonly List<TypeRefRow> _typeRefTable = new List<TypeRefRow>();
        private readonly List<TypeSpecRow> _typeSpecTable = new List<TypeSpecRow>();
        private readonly List<AssemblyRefTableRow> _assemblyRefTable = new List<AssemblyRefTableRow>();
        private readonly List<StandaloneSigRow> _standAloneSigTable = new List<StandaloneSigRow>();

        // debug tables:
        private readonly List<DocumentRow> _documentTable = new List<DocumentRow>();
        private readonly List<MethodDebugInformationRow> _methodDebugInformationTable = new List<MethodDebugInformationRow>();
        private readonly List<LocalScopeRow> _localScopeTable = new List<LocalScopeRow>();
        private readonly List<LocalVariableRow> _localVariableTable = new List<LocalVariableRow>();
        private readonly List<LocalConstantRow> _localConstantTable = new List<LocalConstantRow>();
        private readonly List<ImportScopeRow> _importScopeTable = new List<ImportScopeRow>();
        private readonly List<StateMachineMethodRow> _stateMachineMethodTable = new List<StateMachineMethodRow>();
        private readonly List<CustomDebugInformationRow> _customDebugInformationTable = new List<CustomDebugInformationRow>();

        /// <summary>
        /// Sets the capacity of the specified table. 
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="table"/> is not a valid table index.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="rowCount"/> is negative.</exception>
        /// <remarks>
        /// Use to reduce allocations if the approximate number of rows is known ahead of time.
        /// </remarks>
        public void SetCapacity(TableIndex table, int rowCount)
        {
            if (rowCount < 0)
            {
                Throw.ArgumentOutOfRange(nameof(rowCount));
            }

            switch (table)
            {
                case TableIndex.Module:                 SetTableCapacity(_moduleTable, rowCount); break;
                case TableIndex.TypeRef:                SetTableCapacity(_typeRefTable, rowCount); break;
                case TableIndex.TypeDef:                SetTableCapacity(_typeDefTable, rowCount); break;
                case TableIndex.Field:                  SetTableCapacity(_fieldTable, rowCount); break;
                case TableIndex.MethodDef:              SetTableCapacity(_methodDefTable, rowCount); break;
                case TableIndex.Param:                  SetTableCapacity(_paramTable, rowCount); break;
                case TableIndex.InterfaceImpl:          SetTableCapacity(_interfaceImplTable, rowCount); break;
                case TableIndex.MemberRef:              SetTableCapacity(_memberRefTable, rowCount); break;
                case TableIndex.Constant:               SetTableCapacity(_constantTable, rowCount); break;
                case TableIndex.CustomAttribute:        SetTableCapacity(_customAttributeTable, rowCount); break;
                case TableIndex.FieldMarshal:           SetTableCapacity(_fieldMarshalTable, rowCount); break;
                case TableIndex.DeclSecurity:           SetTableCapacity(_declSecurityTable, rowCount); break;
                case TableIndex.ClassLayout:            SetTableCapacity(_classLayoutTable, rowCount); break;
                case TableIndex.FieldLayout:            SetTableCapacity(_fieldLayoutTable, rowCount); break;
                case TableIndex.StandAloneSig:          SetTableCapacity(_standAloneSigTable, rowCount); break;
                case TableIndex.EventMap:               SetTableCapacity(_eventMapTable, rowCount); break;
                case TableIndex.Event:                  SetTableCapacity(_eventTable, rowCount); break;
                case TableIndex.PropertyMap:            SetTableCapacity(_propertyMapTable, rowCount); break;
                case TableIndex.Property:               SetTableCapacity(_propertyTable, rowCount); break;
                case TableIndex.MethodSemantics:        SetTableCapacity(_methodSemanticsTable, rowCount); break;
                case TableIndex.MethodImpl:             SetTableCapacity(_methodImplTable, rowCount); break;
                case TableIndex.ModuleRef:              SetTableCapacity(_moduleRefTable, rowCount); break;
                case TableIndex.TypeSpec:               SetTableCapacity(_typeSpecTable, rowCount); break;
                case TableIndex.ImplMap:                SetTableCapacity(_implMapTable, rowCount); break;
                case TableIndex.FieldRva:               SetTableCapacity(_fieldRvaTable, rowCount); break;
                case TableIndex.EncLog:                 SetTableCapacity(_encLogTable, rowCount); break;
                case TableIndex.EncMap:                 SetTableCapacity(_encMapTable, rowCount); break;
                case TableIndex.Assembly:               SetTableCapacity(_assemblyTable, rowCount); break;
                case TableIndex.AssemblyRef:            SetTableCapacity(_assemblyRefTable, rowCount); break;
                case TableIndex.File:                   SetTableCapacity(_fileTable, rowCount); break;
                case TableIndex.ExportedType:           SetTableCapacity(_exportedTypeTable, rowCount); break;
                case TableIndex.ManifestResource:       SetTableCapacity(_manifestResourceTable, rowCount); break;
                case TableIndex.NestedClass:            SetTableCapacity(_nestedClassTable, rowCount); break;
                case TableIndex.GenericParam:           SetTableCapacity(_genericParamTable, rowCount); break;
                case TableIndex.MethodSpec:             SetTableCapacity(_methodSpecTable, rowCount); break;
                case TableIndex.GenericParamConstraint: SetTableCapacity(_genericParamConstraintTable, rowCount); break;
                case TableIndex.Document:               SetTableCapacity(_documentTable, rowCount); break;
                case TableIndex.MethodDebugInformation: SetTableCapacity(_methodDebugInformationTable, rowCount); break;
                case TableIndex.LocalScope:             SetTableCapacity(_localScopeTable, rowCount); break;
                case TableIndex.LocalVariable:          SetTableCapacity(_localVariableTable, rowCount); break;
                case TableIndex.LocalConstant:          SetTableCapacity(_localConstantTable, rowCount); break;
                case TableIndex.ImportScope:            SetTableCapacity(_importScopeTable, rowCount); break;
                case TableIndex.StateMachineMethod:     SetTableCapacity(_stateMachineMethodTable, rowCount); break;
                case TableIndex.CustomDebugInformation: SetTableCapacity(_customDebugInformationTable, rowCount); break;

                case TableIndex.AssemblyOS:
                case TableIndex.AssemblyProcessor:
                case TableIndex.AssemblyRefOS:
                case TableIndex.AssemblyRefProcessor:
                case TableIndex.EventPtr:
                case TableIndex.FieldPtr:
                case TableIndex.MethodPtr:
                case TableIndex.ParamPtr:
                case TableIndex.PropertyPtr:
                    // these tables are currently not serialized
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(table));
            }
        }

        private static void SetTableCapacity<T>(List<T> table, int rowCount)
        {
            if (rowCount > table.Count)
            {
                table.Capacity = rowCount;
            }
        }

        #region Building

        public ModuleDefinitionHandle AddModule(
                int generation,
                StringHandle moduleName,
                GuidHandle mvid,
                GuidHandle encId,
                GuidHandle encBaseId)
        {
            _moduleTable.Add(new ModuleRow
            {
                Generation = (ushort)generation,
                Name = moduleName,
                ModuleVersionId = mvid,
                EncId = encId,
                EncBaseId = encBaseId,
            });

            return EntityHandle.ModuleDefinition;
        }

        public AssemblyDefinitionHandle AddAssembly(
            StringHandle name, 
            Version version,
            StringHandle culture,
            BlobHandle publicKey,
            AssemblyFlags flags,
            AssemblyHashAlgorithm hashAlgorithm)
        {
            _assemblyTable.Add(new AssemblyRow
            {
                Flags = (ushort)flags,
                HashAlgorithm = (uint)hashAlgorithm,
                Version = version,
                AssemblyKey = publicKey,
                AssemblyName = name,
                AssemblyCulture = culture
            });

            return EntityHandle.AssemblyDefinition;
        }

        public AssemblyReferenceHandle AddAssemblyReference(
            StringHandle name,
            Version version,
            StringHandle culture,
            BlobHandle publicKeyOrToken,
            AssemblyFlags flags,
            BlobHandle hashValue)
        {
            if (version == null)
            {
                Throw.ArgumentNull(nameof(version));
            }

            _assemblyRefTable.Add(new AssemblyRefTableRow
            {
                Name = name,
                Version = version,
                Culture = culture,
                PublicKeyToken = publicKeyOrToken,
                Flags = (uint)flags,
                HashValue = hashValue
            });

            return AssemblyReferenceHandle.FromRowId(_assemblyRefTable.Count);
        }

        /// <summary>
        /// Adds a type definition.
        /// </summary>
        /// <param name="attributes">Attributes</param>
        /// <param name="namespace">Namespace</param>
        /// <param name="name">Type name</param>
        /// <param name="baseType"><see cref="TypeDefinitionHandle"/>, <see cref="TypeReferenceHandle"/>, <see cref="TypeSpecificationHandle"/> or nil.</param>
        /// <param name="fieldList">
        /// If the type declares fields the handle of the first one, otherwise the handle of the first field declared by the next type definition.
        /// If no type defines any fields in the module the <see cref="MetadataTokens.FieldDefinitionHandle(int)"/>(1).
        /// </param>
        /// <param name="methodList">
        /// If the type declares methods the handle of the first one, otherwise the handle of the first method declared by the next type definition.
        /// If no type defines any methods in the module the <see cref="MetadataTokens.MethodDefinitionHandle(int)"/>(1).
        /// </param>
        /// <exception cref="ArgumentException"><paramref name="baseType"/> doesn't have the expected handle kind.</exception>
        public TypeDefinitionHandle AddTypeDefinition(
            TypeAttributes attributes, 
            StringHandle @namespace,
            StringHandle name,
            EntityHandle baseType,
            FieldDefinitionHandle fieldList,
            MethodDefinitionHandle methodList)
        {
            Debug.Assert(@namespace != null);
            Debug.Assert(name != null);

            _typeDefTable.Add(new TypeDefRow
            {
                Flags = (uint)attributes,
                Name = name,
                Namespace = @namespace,
                Extends = baseType.IsNil ? 0 : CodedIndex.ToTypeDefOrRefOrSpec(baseType),
                FieldList = fieldList.RowId,
                MethodList = methodList.RowId
            });

            return TypeDefinitionHandle.FromRowId(_typeDefTable.Count);
        }

        public void AddTypeLayout(
            TypeDefinitionHandle type,
            ushort packingSize,
            uint size)
        {
            _classLayoutTable.Add(new ClassLayoutRow
            {
                Parent = type.RowId,
                PackingSize = packingSize,
                ClassSize = size
            });
        }

        /// <summary>
        /// Adds an interface implementation to a type.
        /// </summary>
        /// <param name="type">The type implementing the interface.</param>
        /// <param name="implementedInterface">
        /// The interface being implemented: 
        /// <see cref="TypeDefinitionHandle"/>, <see cref="TypeReferenceHandle"/> or <see cref="TypeSpecificationHandle"/>.
        /// </param>
        /// <exception cref="ArgumentException"><paramref name="implementedInterface"/> doesn't have the expected handle kind.</exception>
        public InterfaceImplementationHandle AddInterfaceImplementation(
            TypeDefinitionHandle type,
            EntityHandle implementedInterface)
        {
            _interfaceImplTable.Add(new InterfaceImplRow
            {
                Class = type.RowId,
                Interface = CodedIndex.ToTypeDefOrRefOrSpec(implementedInterface)
            });

            return InterfaceImplementationHandle.FromRowId(_interfaceImplTable.Count);
        }

        public void AddNestedType(
            TypeDefinitionHandle type,
            TypeDefinitionHandle enclosingType)
        {
            _nestedClassTable.Add(new NestedClassRow
            {
                NestedClass = type.RowId,
                EnclosingClass = enclosingType.RowId
            });
        }

        /// <summary>
        /// Add a type reference.
        /// </summary>
        /// <param name="resolutionScope">
        /// The entity declaring the target type: 
        /// <see cref="ModuleDefinitionHandle"/>, <see cref="ModuleReferenceHandle"/>, <see cref="AssemblyReferenceHandle"/> or <see cref="TypeReferenceHandle"/>.
        /// </param>
        /// <param name="namespace">Namespace.</param>
        /// <param name="name">Type name.</param>
        /// <exception cref="ArgumentException"><paramref name="resolutionScope"/> doesn't have the expected handle kind.</exception>
        public TypeReferenceHandle AddTypeReference(
            EntityHandle resolutionScope, 
            StringHandle @namespace, 
            StringHandle name)
        {
            Debug.Assert(@namespace != null);
            Debug.Assert(name != null);

            _typeRefTable.Add(new TypeRefRow
            {
                ResolutionScope = CodedIndex.ToResolutionScope(resolutionScope),
                Name = name,
                Namespace = @namespace
            });

            return TypeReferenceHandle.FromRowId(_typeRefTable.Count);
        }

        public TypeSpecificationHandle AddTypeSpecification(BlobHandle signature)
        {
            _typeSpecTable.Add(new TypeSpecRow
            {
                Signature = signature
            });

            return TypeSpecificationHandle.FromRowId(_typeSpecTable.Count);
        }

        public StandaloneSignatureHandle AddStandaloneSignature(BlobHandle signature)
        {
            _standAloneSigTable.Add(new StandaloneSigRow
            {
                Signature = signature
            });

            return StandaloneSignatureHandle.FromRowId(_standAloneSigTable.Count);
        }

        /// <summary>
        /// Adds a property defintion.
        /// </summary>
        /// <param name="attributes">Attributes</param>
        /// <param name="name">Name</param>
        /// <param name="signature">Signature of the property.</param>
        public PropertyDefinitionHandle AddProperty(PropertyAttributes attributes, StringHandle name, BlobHandle signature)
        {
            _propertyTable.Add(new PropertyRow
            {
                PropFlags = (ushort)attributes,
                Name = name,
                Type = signature
            });

            return PropertyDefinitionHandle.FromRowId(_propertyTable.Count);
        }

        public void AddPropertyMap(TypeDefinitionHandle declaringType, PropertyDefinitionHandle propertyList)
        {
            _propertyMapTable.Add(new PropertyMapRow
            {
                Parent = declaringType.RowId,
                PropertyList = propertyList.RowId
            });
        }

        /// <summary>
        /// Adds an event defintion.
        /// </summary>
        /// <param name="attributes">Attributes</param>
        /// <param name="name">Name</param>
        /// <param name="type">Type of the event: <see cref="TypeDefinitionHandle"/>, <see cref="TypeReferenceHandle"/>, or <see cref="TypeSpecificationHandle"/></param>
        /// <exception cref="ArgumentException"><paramref name="type"/> doesn't have the expected handle kind.</exception>
        public EventDefinitionHandle AddEvent(EventAttributes attributes, StringHandle name, EntityHandle type)
        {
            _eventTable.Add(new EventRow
            {
                EventFlags = (ushort)attributes,
                Name = name,
                EventType = CodedIndex.ToTypeDefOrRefOrSpec(type)
            });

            return EventDefinitionHandle.FromRowId(_eventTable.Count);
        }

        public void AddEventMap(TypeDefinitionHandle declaringType, EventDefinitionHandle eventList)
        {
            _eventMapTable.Add(new EventMapRow
            {
                Parent = declaringType.RowId,
                EventList = eventList.RowId
            });
        }

        /// <summary>
        /// Adds a default value for a parameter, field or property.
        /// </summary>
        /// <param name="parent"><see cref="ParameterHandle"/>, <see cref="FieldDefinitionHandle"/>, or <see cref="PropertyDefinitionHandle"/></param>
        /// <param name="value">The constant value.</param>
        /// <exception cref="ArgumentException"><paramref name="parent"/> doesn't have the expected handle kind.</exception>
        public ConstantHandle AddConstant(EntityHandle parent, object value)
        {
            int parentCodedIndex = CodedIndex.ToHasConstant(parent);

            // the table is required to be sorted by Parent:
            _constantTableNeedsSorting |= parentCodedIndex < _constantTableLastParent;
            _constantTableLastParent = parentCodedIndex;

            _constantTable.Add(new ConstantRow
            {
                Type = (byte)MetadataWriterUtilities.GetConstantTypeCode(value),
                Parent = parentCodedIndex,
                Value = GetOrAddConstantBlob(value)
            });

            return ConstantHandle.FromRowId(_constantTable.Count);
        }

        /// <summary>
        /// Associates a method (a getter, a setter, an adder, etc.) with a property or an event.
        /// </summary>
        /// <param name="association"><see cref="EventDefinitionHandle"/> or <see cref="PropertyDefinitionHandle"/>.</param>
        /// <param name="semantics">Semantics.</param>
        /// <param name="methodDefinition">Method definition.</param>
        /// <exception cref="ArgumentException"><paramref name="association"/> doesn't have the expected handle kind.</exception>
        public void AddMethodSemantics(EntityHandle association, MethodSemanticsAttributes semantics, MethodDefinitionHandle methodDefinition)
        {
            int associationCodedIndex = CodedIndex.ToHasSemantics(association);

            // the table is required to be sorted by Association:
            _methodSemanticsTableNeedsSorting |= associationCodedIndex < _methodSemanticsTableLastAssociation;
            _methodSemanticsTableLastAssociation = associationCodedIndex;

            _methodSemanticsTable.Add(new MethodSemanticsRow
            {
                Association = associationCodedIndex,
                Method = methodDefinition.RowId,
                Semantic = (ushort)semantics
            });
        }

        /// <summary>
        /// Add a custom attribute.
        /// </summary>
        /// <param name="parent">
        /// An entity to attach the custom attribute to: 
        /// <see cref="MethodDefinitionHandle"/>,
        /// <see cref="FieldDefinitionHandle"/>,
        /// <see cref="TypeReferenceHandle"/>,
        /// <see cref="TypeDefinitionHandle"/>,
        /// <see cref="ParameterHandle"/>,
        /// <see cref="InterfaceImplementationHandle"/>,
        /// <see cref="MemberReferenceHandle"/>,
        /// <see cref="ModuleDefinitionHandle"/>,
        /// <see cref="DeclarativeSecurityAttributeHandle"/>,
        /// <see cref="PropertyDefinitionHandle"/>,
        /// <see cref="EventDefinitionHandle"/>,
        /// <see cref="StandaloneSignatureHandle"/>,
        /// <see cref="ModuleReferenceHandle"/>,
        /// <see cref="TypeSpecificationHandle"/>,
        /// <see cref="AssemblyDefinitionHandle"/>,
        /// <see cref="AssemblyReferenceHandle"/>,
        /// <see cref="AssemblyFileHandle"/>,
        /// <see cref="ExportedTypeHandle"/>,
        /// <see cref="ManifestResourceHandle"/>,
        /// <see cref="GenericParameterHandle"/>,
        /// <see cref="GenericParameterConstraintHandle"/> or
        /// <see cref="MethodSpecificationHandle"/>.
        /// </param>
        /// <param name="constructor">
        /// Custom attribute constructor: <see cref="MethodDefinitionHandle"/> or <see cref="MemberReferenceHandle"/>
        /// </param>
        /// <param name="value">
        /// Custom attribute value blob.
        /// </param>
        /// <exception cref="ArgumentException"><paramref name="parent"/> doesn't have the expected handle kind.</exception>
        public CustomAttributeHandle AddCustomAttribute(EntityHandle parent, EntityHandle constructor, BlobHandle value)
        {
            int parentCodedIndex = CodedIndex.ToHasCustomAttribute(parent);

            // the table is required to be sorted by Parent:
            _customAttributeTableNeedsSorting |= parentCodedIndex < _customAttributeTableLastParent;
            _customAttributeTableLastParent = parentCodedIndex;

            _customAttributeTable.Add(new CustomAttributeRow
            {
                Parent = parentCodedIndex,
                Type = CodedIndex.ToCustomAttributeType(constructor),
                Value = value
            });

            return CustomAttributeHandle.FromRowId(_customAttributeTable.Count);
        }

        /// <summary>
        /// Adds a method specification (instantiation).
        /// </summary>
        /// <param name="method">Generic method: <see cref="MethodDefinitionHandle"/> or <see cref="MemberReferenceHandle"/></param>
        /// <param name="instantiation">Instantiation blob encoding the generic arguments of the method.</param>
        /// <exception cref="ArgumentException"><paramref name="method"/> doesn't have the expected handle kind.</exception>
        public MethodSpecificationHandle AddMethodSpecification(EntityHandle method, BlobHandle instantiation)
        {
            _methodSpecTable.Add(new MethodSpecRow
            {
                Method = CodedIndex.ToMethodDefOrRef(method),
                Instantiation = instantiation
            });

            return MethodSpecificationHandle.FromRowId(_methodSpecTable.Count);
        }

        public ModuleReferenceHandle AddModuleReference(StringHandle moduleName)
        {
            _moduleRefTable.Add(new ModuleRefRow
            {
                Name = moduleName
            });

            return ModuleReferenceHandle.FromRowId(_moduleRefTable.Count);
        }

        public ParameterHandle AddParameter(ParameterAttributes attributes, StringHandle name, int sequenceNumber)
        {
            _paramTable.Add(new ParamRow
            {
                Flags = (ushort)attributes,
                Name = name,
                Sequence = (ushort)sequenceNumber
            });

            return ParameterHandle.FromRowId(_paramTable.Count);
        }

        /// <summary>
        /// Adds a generic parameter definition.
        /// </summary>
        /// <param name="parent"><see cref="TypeDefinitionHandle"/> or <see cref="MethodDefinitionHandle"/></param>
        /// <param name="attributes">Attributes.</param>
        /// <param name="name">Parameter name.</param>
        /// <param name="index">Zero-based parameter index.</param>
        /// <exception cref="ArgumentException"><paramref name="parent"/> doesn't have the expected handle kind.</exception>
        public GenericParameterHandle AddGenericParameter(
            EntityHandle parent,
            GenericParameterAttributes attributes,
            StringHandle name,
            int index)
        {
            _genericParamTable.Add(new GenericParamRow
            {
                Flags = (ushort)attributes,
                Name = name,
                Number = (ushort)index,
                Owner = CodedIndex.ToTypeOrMethodDef(parent)
            });

            return GenericParameterHandle.FromRowId(_genericParamTable.Count);
        }

        /// <summary>
        /// Adds a type constraint to a generic parameter.
        /// </summary>
        /// <param name="genericParameter">Generic parameter to constrain.</param>
        /// <param name="constraint">Type constraint: <see cref="TypeDefinitionHandle"/>, <see cref="TypeReferenceHandle"/> or <see cref="TypeSpecificationHandle"/></param>
        /// <exception cref="ArgumentException"><paramref name="genericParameter"/> doesn't have the expected handle kind.</exception>
        public GenericParameterConstraintHandle AddGenericParameterConstraint(
            GenericParameterHandle genericParameter,
            EntityHandle constraint)
        {
            _genericParamConstraintTable.Add(new GenericParamConstraintRow
            {
                Owner = genericParameter.RowId,
                Constraint = CodedIndex.ToTypeDefOrRefOrSpec(constraint),
            });

            return GenericParameterConstraintHandle.FromRowId(_genericParamConstraintTable.Count);
        }

        public FieldDefinitionHandle AddFieldDefinition(
            FieldAttributes attributes,
            StringHandle name,
            BlobHandle signature)
        {
            _fieldTable.Add(new FieldDefRow
            {
                Flags = (ushort)attributes,
                Name = name,
                Signature = signature
            });

            return FieldDefinitionHandle.FromRowId(_fieldTable.Count);
        }

        public void AddFieldLayout(
            FieldDefinitionHandle field,
            int offset)
        {
            _fieldLayoutTable.Add(new FieldLayoutRow
            {
                Field = field.RowId,
                Offset = offset
            });
        }

        /// <summary>
        /// Add marshalling information to a field or a parameter.
        /// </summary>
        /// <param name="parent"><see cref="ParameterHandle"/> or <see cref="FieldDefinitionHandle"/>.</param>
        /// <param name="descriptor">Descriptor.</param>
        /// <exception cref="ArgumentException"><paramref name="parent"/> doesn't have the expected handle kind.</exception>
        public void AddMarshallingDescriptor(
            EntityHandle parent,
            BlobHandle descriptor)
        {
            int codedIndex = CodedIndex.ToHasFieldMarshal(parent);

            // the table is required to be sorted by Parent:
            _fieldMarshalTableNeedsSorting |= codedIndex < _fieldMarshalTableLastParent;
            _fieldMarshalTableLastParent = codedIndex;

            _fieldMarshalTable.Add(new FieldMarshalRow
            {
                Parent = codedIndex,
                NativeType = descriptor
            });
        }

        public void AddFieldRelativeVirtualAddress(
            FieldDefinitionHandle field,
            int relativeVirtualAddress)
        {
            _fieldRvaTable.Add(new FieldRvaRow
            {
                Field = field.RowId,
                Offset = (uint)relativeVirtualAddress
            });
        }

        public MethodDefinitionHandle AddMethodDefinition(
            MethodAttributes attributes, 
            MethodImplAttributes implAttributes,
            StringHandle name,
            BlobHandle signature,
            int bodyOffset,
            ParameterHandle paramList)
        {
            _methodDefTable.Add(new MethodRow
            {
                Flags = (ushort)attributes,
                ImplFlags = (ushort)implAttributes,
                Name = name,
                Signature = signature,
                BodyOffset = bodyOffset,
                ParamList = paramList.RowId
            });

            return MethodDefinitionHandle.FromRowId(_methodDefTable.Count);
        }

        /// <summary>
        /// Adds import information to a method definition (P/Invoke).
        /// </summary>
        /// <param name="method">Method definition.</param>
        /// <param name="attributes">Attributes</param>
        /// <param name="name">Unmanaged method name.</param>
        /// <param name="module">Module containing the unmanaged method.</param>
        public void AddMethodImport(
            MethodDefinitionHandle method,
            MethodImportAttributes attributes, 
            StringHandle name, 
            ModuleReferenceHandle module)
        {
            _implMapTable.Add(new ImplMapRow
            {
                MemberForwarded = CodedIndex.ToMemberForwarded(method),
                ImportName = name,
                ImportScope = module.RowId,
                MappingFlags = (ushort)attributes,
            });
        }

        /// <summary>
        /// Defines an implementation for a method declaration within a type.
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="methodBody"><see cref="MethodDefinitionHandle"/> or <see cref="MemberReferenceHandle"/> which provides the implementation.</param>
        /// <param name="methodDeclaration"><see cref="MethodDefinitionHandle"/> or <see cref="MemberReferenceHandle"/> the method being implemented.</param>
        /// <exception cref="ArgumentException"><paramref name="methodBody"/> or <paramref name="methodDeclaration"/> doesn't have the expected handle kind.</exception>
        public MethodImplementationHandle AddMethodImplementation(
            TypeDefinitionHandle type,
            EntityHandle methodBody,
            EntityHandle methodDeclaration)
        {
            _methodImplTable.Add(new MethodImplRow
            {
                Class = type.RowId,
                MethodBody = CodedIndex.ToMethodDefOrRef(methodBody),
                MethodDecl = CodedIndex.ToMethodDefOrRef(methodDeclaration)
            });

            return MethodImplementationHandle.FromRowId(_methodImplTable.Count);
        }

        /// <summary>
        /// Adds a MemberRef table row.
        /// </summary>
        /// <param name="parent">Containing entity:
        /// <see cref="TypeDefinitionHandle"/>, 
        /// <see cref="TypeReferenceHandle"/>, 
        /// <see cref="ModuleReferenceHandle"/>,
        /// <see cref="MethodDefinitionHandle"/>, or 
        /// <see cref="TypeSpecificationHandle"/>.
        /// </param>
        /// <param name="name">Member name.</param>
        /// <param name="signature">Member signature.</param>
        /// <exception cref="ArgumentException"><paramref name="parent"/> doesn't have the expected handle kind.</exception>
        public MemberReferenceHandle AddMemberReference(
            EntityHandle parent,
            StringHandle name,
            BlobHandle signature)
        {
            _memberRefTable.Add(new MemberRefRow
            {
                Class = CodedIndex.ToMemberRefParent(parent),
                Name = name,
                Signature = signature
            });

            return MemberReferenceHandle.FromRowId(_memberRefTable.Count);
        }

        /// <summary>
        /// Adds a manifest resource.
        /// </summary>
        /// <param name="attributes">Attributes</param>
        /// <param name="name">Resource name</param>
        /// <param name="implementation"><see cref="AssemblyFileHandle"/>, <see cref="AssemblyReferenceHandle"/>, or nil</param>
        /// <param name="offset">Specifies the byte offset within the referenced file at which this resource record begins.</param>
        /// <exception cref="ArgumentException"><paramref name="implementation"/> doesn't have the expected handle kind.</exception>
        public ManifestResourceHandle AddManifestResource(
            ManifestResourceAttributes attributes,
            StringHandle name,
            EntityHandle implementation,
            uint offset)
        {
            _manifestResourceTable.Add(new ManifestResourceRow
            {
                Flags = (uint)attributes,
                Name = name,
                Implementation = implementation.IsNil ? 0 : CodedIndex.ToImplementation(implementation),
                Offset = offset
            });

            return ManifestResourceHandle.FromRowId(_manifestResourceTable.Count);
        }

        public AssemblyFileHandle AddAssemblyFile(
            StringHandle name,
            BlobHandle hashValue,
            bool containsMetadata)
        {
            _fileTable.Add(new FileTableRow
            {
                FileName = name,
                Flags = containsMetadata ? 0u : 1u,
                HashValue = hashValue
            });

            return AssemblyFileHandle.FromRowId(_fileTable.Count);
        }

        /// <summary>
        /// Adds an exported type.
        /// </summary>
        /// <param name="attributes">Attributes</param>
        /// <param name="namespace">Namespace</param>
        /// <param name="name">Type name</param>
        /// <param name="implementation"><see cref="AssemblyFileHandle"/>, <see cref="ExportedTypeHandle"/> or <see cref="AssemblyReferenceHandle"/></param>
        /// <param name="typeDefinitionId">Type definition id</param>
        /// <exception cref="ArgumentException"><paramref name="implementation"/> doesn't have the expected handle kind.</exception>
        public ExportedTypeHandle AddExportedType(
            TypeAttributes attributes,
            StringHandle @namespace,
            StringHandle name,
            EntityHandle implementation,
            int typeDefinitionId)
        {
            _exportedTypeTable.Add(new ExportedTypeRow
            {
                Flags = (uint)attributes,
                Implementation = CodedIndex.ToImplementation(implementation),
                TypeNamespace = @namespace,
                TypeName = name,
                TypeDefId = typeDefinitionId
            });

            return ExportedTypeHandle.FromRowId(_exportedTypeTable.Count);
        }

        // TODO: remove
        public uint GetExportedTypeFlags(int rowId)
        {
            return _exportedTypeTable[rowId].Flags;
        }

        /// <summary>
        /// Adds declarative security attribute to a type, method or an assembly.
        /// </summary>
        /// <param name="parent"><see cref="TypeDefinitionHandle"/>, <see cref="MethodDefinitionHandle"/>, or <see cref="AssemblyDefinitionHandle"/></param>
        /// <param name="action">Security action</param>
        /// <param name="permissionSet">Permission set blob.</param>
        /// <exception cref="ArgumentException"><paramref name="parent"/> doesn't have the expected handle kind.</exception>
        public DeclarativeSecurityAttributeHandle AddDeclarativeSecurityAttribute(
            EntityHandle parent,
            DeclarativeSecurityAction action,
            BlobHandle permissionSet)
        {
            int parentCodedIndex = CodedIndex.ToHasDeclSecurity(parent);

            // the table is required to be sorted by Parent:
            _declSecurityTableNeedsSorting |= parentCodedIndex < _declSecurityTableLastParent;
            _declSecurityTableLastParent = parentCodedIndex;

            _declSecurityTable.Add(new DeclSecurityRow
            {
                Parent = parentCodedIndex,
                Action = (ushort)action,
                PermissionSet = permissionSet
            });

            return DeclarativeSecurityAttributeHandle.FromRowId(_declSecurityTable.Count);
        }

        public void AddEncLogEntry(EntityHandle entity, EditAndContinueOperation code)
        {
            _encLogTable.Add(new EncLogRow
            {
                Token = entity.Token,
                FuncCode = (byte)code
            });
        }

        public void AddEncMapEntry(EntityHandle entity)
        {
            _encMapTable.Add(new EncMapRow
            {
                Token = entity.Token
            });
        }

        public DocumentHandle AddDocument(BlobHandle name, GuidHandle hashAlgorithm, BlobHandle hash, GuidHandle language)
        {
            _documentTable.Add(new DocumentRow
            {
                Name = name,
                HashAlgorithm = hashAlgorithm,
                Hash = hash,
                Language = language
            });

            return DocumentHandle.FromRowId(_documentTable.Count);
        }

        public MethodDebugInformationHandle AddMethodDebugInformation(DocumentHandle document, BlobHandle sequencePoints)
        {
            _methodDebugInformationTable.Add(new MethodDebugInformationRow
            {
                Document = document.RowId,
                SequencePoints = sequencePoints
            });

            return MethodDebugInformationHandle.FromRowId(_methodDebugInformationTable.Count);
        }

        public LocalScopeHandle AddLocalScope(MethodDefinitionHandle method, ImportScopeHandle importScope, LocalVariableHandle variableList, LocalConstantHandle constantList, int startOffset, int length)
        {
            _localScopeTable.Add(new LocalScopeRow
            {
                Method = method.RowId,
                ImportScope = importScope.RowId,
                VariableList = variableList.RowId,
                ConstantList = constantList.RowId,
                StartOffset = startOffset,
                Length = length
            });

            return LocalScopeHandle.FromRowId(_localScopeTable.Count);
        }

        public LocalVariableHandle AddLocalVariable(LocalVariableAttributes attributes, int index, StringHandle name)
        {
            _localVariableTable.Add(new LocalVariableRow
            {
                Attributes = (ushort)attributes,
                Index = (ushort)index,
                Name = name
            });

            return LocalVariableHandle.FromRowId(_localVariableTable.Count);
        }
        
        public LocalConstantHandle AddLocalConstant(StringHandle name, BlobHandle signature)
        {
            _localConstantTable.Add(new LocalConstantRow
            {
                Name = name,
                Signature = signature
            });

            return LocalConstantHandle.FromRowId(_localConstantTable.Count);
        }

        public ImportScopeHandle AddImportScope(ImportScopeHandle parentScope, BlobHandle imports)
        {
            _importScopeTable.Add(new ImportScopeRow
            {
                Parent = parentScope.RowId,
                Imports = imports
            });

            return ImportScopeHandle.FromRowId(_importScopeTable.Count);
        }

        public void AddStateMachineMethod(MethodDefinitionHandle moveNextMethod, MethodDefinitionHandle kickoffMethod)
        {
            _stateMachineMethodTable.Add(new StateMachineMethodRow
            {
                MoveNextMethod  = moveNextMethod.RowId,
                KickoffMethod = kickoffMethod.RowId
            });
        }

        /// <summary>
        /// Add a custom debug information.
        /// </summary>
        /// <param name="parent">
        /// An entity to attach the debug information to: 
        /// <see cref="MethodDefinitionHandle"/>,
        /// <see cref="FieldDefinitionHandle"/>,
        /// <see cref="TypeReferenceHandle"/>,
        /// <see cref="TypeDefinitionHandle"/>,
        /// <see cref="ParameterHandle"/>,
        /// <see cref="InterfaceImplementationHandle"/>,
        /// <see cref="MemberReferenceHandle"/>,
        /// <see cref="ModuleDefinitionHandle"/>,
        /// <see cref="DeclarativeSecurityAttributeHandle"/>,
        /// <see cref="PropertyDefinitionHandle"/>,
        /// <see cref="EventDefinitionHandle"/>,
        /// <see cref="StandaloneSignatureHandle"/>,
        /// <see cref="ModuleReferenceHandle"/>,
        /// <see cref="TypeSpecificationHandle"/>,
        /// <see cref="AssemblyDefinitionHandle"/>,
        /// <see cref="AssemblyReferenceHandle"/>,
        /// <see cref="AssemblyFileHandle"/>,
        /// <see cref="ExportedTypeHandle"/>,
        /// <see cref="ManifestResourceHandle"/>,
        /// <see cref="GenericParameterHandle"/>,
        /// <see cref="GenericParameterConstraintHandle"/>,
        /// <see cref="MethodSpecificationHandle"/>,
        /// <see cref="DocumentHandle"/>,
        /// <see cref="LocalScopeHandle"/>,
        /// <see cref="LocalVariableHandle"/>,
        /// <see cref="LocalConstantHandle"/> or
        /// <see cref="ImportScopeHandle"/>.
        /// </param>
        /// <param name="kind">Information kind. Determines the structure of the <paramref name="value"/> blob.</param>
        /// <param name="value">Custom debug information blob.</param>
        /// <exception cref="ArgumentException"><paramref name="parent"/> doesn't have the expected handle kind.</exception>
        public CustomDebugInformationHandle AddCustomDebugInformation(EntityHandle parent, GuidHandle kind, BlobHandle value)
        {
            _customDebugInformationTable.Add(new CustomDebugInformationRow
            {
                Parent = CodedIndex.ToHasCustomDebugInformation(parent),
                Kind = kind,
                Value = value
            });

            return CustomDebugInformationHandle.FromRowId(_customDebugInformationTable.Count);
        }

        #endregion

        public ImmutableArray<int> GetRowCounts()
        {
            var rowCounts = new int[MetadataTokens.TableCount];

            rowCounts[(int)TableIndex.Assembly] = _assemblyTable.Count;
            rowCounts[(int)TableIndex.AssemblyRef] = _assemblyRefTable.Count;
            rowCounts[(int)TableIndex.ClassLayout] = _classLayoutTable.Count;
            rowCounts[(int)TableIndex.Constant] = _constantTable.Count;
            rowCounts[(int)TableIndex.CustomAttribute] = _customAttributeTable.Count;
            rowCounts[(int)TableIndex.DeclSecurity] = _declSecurityTable.Count;
            rowCounts[(int)TableIndex.EncLog] = _encLogTable.Count;
            rowCounts[(int)TableIndex.EncMap] = _encMapTable.Count;
            rowCounts[(int)TableIndex.EventMap] = _eventMapTable.Count;
            rowCounts[(int)TableIndex.Event] = _eventTable.Count;
            rowCounts[(int)TableIndex.ExportedType] = _exportedTypeTable.Count;
            rowCounts[(int)TableIndex.FieldLayout] = _fieldLayoutTable.Count;
            rowCounts[(int)TableIndex.FieldMarshal] = _fieldMarshalTable.Count;
            rowCounts[(int)TableIndex.FieldRva] = _fieldRvaTable.Count;
            rowCounts[(int)TableIndex.Field] = _fieldTable.Count;
            rowCounts[(int)TableIndex.File] = _fileTable.Count;
            rowCounts[(int)TableIndex.GenericParamConstraint] = _genericParamConstraintTable.Count;
            rowCounts[(int)TableIndex.GenericParam] = _genericParamTable.Count;
            rowCounts[(int)TableIndex.ImplMap] = _implMapTable.Count;
            rowCounts[(int)TableIndex.InterfaceImpl] = _interfaceImplTable.Count;
            rowCounts[(int)TableIndex.ManifestResource] = _manifestResourceTable.Count;
            rowCounts[(int)TableIndex.MemberRef] = _memberRefTable.Count;
            rowCounts[(int)TableIndex.MethodImpl] = _methodImplTable.Count;
            rowCounts[(int)TableIndex.MethodSemantics] = _methodSemanticsTable.Count;
            rowCounts[(int)TableIndex.MethodSpec] = _methodSpecTable.Count;
            rowCounts[(int)TableIndex.MethodDef] = _methodDefTable.Count;
            rowCounts[(int)TableIndex.ModuleRef] = _moduleRefTable.Count;
            rowCounts[(int)TableIndex.Module] = _moduleTable.Count;
            rowCounts[(int)TableIndex.NestedClass] = _nestedClassTable.Count;
            rowCounts[(int)TableIndex.Param] = _paramTable.Count;
            rowCounts[(int)TableIndex.PropertyMap] = _propertyMapTable.Count;
            rowCounts[(int)TableIndex.Property] = _propertyTable.Count;
            rowCounts[(int)TableIndex.StandAloneSig] = _standAloneSigTable.Count;
            rowCounts[(int)TableIndex.TypeDef] = _typeDefTable.Count;
            rowCounts[(int)TableIndex.TypeRef] = _typeRefTable.Count;
            rowCounts[(int)TableIndex.TypeSpec] = _typeSpecTable.Count;

            rowCounts[(int)TableIndex.Document] = _documentTable.Count;
            rowCounts[(int)TableIndex.MethodDebugInformation] = _methodDebugInformationTable.Count;
            rowCounts[(int)TableIndex.LocalScope] = _localScopeTable.Count;
            rowCounts[(int)TableIndex.LocalVariable] = _localVariableTable.Count;
            rowCounts[(int)TableIndex.LocalConstant] = _localConstantTable.Count;
            rowCounts[(int)TableIndex.StateMachineMethod] = _stateMachineMethodTable.Count;
            rowCounts[(int)TableIndex.ImportScope] = _importScopeTable.Count;
            rowCounts[(int)TableIndex.CustomDebugInformation] = _customDebugInformationTable.Count;

            return ImmutableArray.CreateRange(rowCounts);
        }

        #region Serialization

        internal void SerializeMetadataTables(
            BlobBuilder writer,
            MetadataSizes metadataSizes,
            int methodBodyStreamRva,
            int mappedFieldDataStreamRva)
        {
            int startPosition = writer.Count;

            this.SerializeTablesHeader(writer, metadataSizes);

            if (metadataSizes.IsPresent(TableIndex.Module))
            {
                SerializeModuleTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.TypeRef))
            {
                this.SerializeTypeRefTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.TypeDef))
            {
                this.SerializeTypeDefTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.Field))
            {
                this.SerializeFieldTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.MethodDef))
            {
                this.SerializeMethodDefTable(writer, metadataSizes, methodBodyStreamRva);
            }

            if (metadataSizes.IsPresent(TableIndex.Param))
            {
                this.SerializeParamTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.InterfaceImpl))
            {
                this.SerializeInterfaceImplTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.MemberRef))
            {
                this.SerializeMemberRefTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.Constant))
            {
                this.SerializeConstantTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.CustomAttribute))
            {
                this.SerializeCustomAttributeTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.FieldMarshal))
            {
                this.SerializeFieldMarshalTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.DeclSecurity))
            {
                this.SerializeDeclSecurityTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.ClassLayout))
            {
                this.SerializeClassLayoutTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.FieldLayout))
            {
                this.SerializeFieldLayoutTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.StandAloneSig))
            {
                this.SerializeStandAloneSigTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.EventMap))
            {
                this.SerializeEventMapTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.Event))
            {
                this.SerializeEventTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.PropertyMap))
            {
                this.SerializePropertyMapTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.Property))
            {
                this.SerializePropertyTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.MethodSemantics))
            {
                this.SerializeMethodSemanticsTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.MethodImpl))
            {
                this.SerializeMethodImplTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.ModuleRef))
            {
                this.SerializeModuleRefTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.TypeSpec))
            {
                this.SerializeTypeSpecTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.ImplMap))
            {
                this.SerializeImplMapTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.FieldRva))
            {
                this.SerializeFieldRvaTable(writer, metadataSizes, mappedFieldDataStreamRva);
            }

            if (metadataSizes.IsPresent(TableIndex.EncLog))
            {
                this.SerializeEncLogTable(writer);
            }

            if (metadataSizes.IsPresent(TableIndex.EncMap))
            {
                this.SerializeEncMapTable(writer);
            }

            if (metadataSizes.IsPresent(TableIndex.Assembly))
            {
                this.SerializeAssemblyTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.AssemblyRef))
            {
                this.SerializeAssemblyRefTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.File))
            {
                this.SerializeFileTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.ExportedType))
            {
                this.SerializeExportedTypeTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.ManifestResource))
            {
                this.SerializeManifestResourceTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.NestedClass))
            {
                this.SerializeNestedClassTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.GenericParam))
            {
                this.SerializeGenericParamTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.MethodSpec))
            {
                this.SerializeMethodSpecTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.GenericParamConstraint))
            {
                this.SerializeGenericParamConstraintTable(writer, metadataSizes);
            }

            // debug tables
            if (metadataSizes.IsPresent(TableIndex.Document))
            {
                this.SerializeDocumentTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.MethodDebugInformation))
            {
                this.SerializeMethodDebugInformationTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.LocalScope))
            {
                this.SerializeLocalScopeTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.LocalVariable))
            {
                this.SerializeLocalVariableTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.LocalConstant))
            {
                this.SerializeLocalConstantTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.ImportScope))
            {
                this.SerializeImportScopeTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.StateMachineMethod))
            {
                this.SerializeStateMachineMethodTable(writer, metadataSizes);
            }

            if (metadataSizes.IsPresent(TableIndex.CustomDebugInformation))
            {
                this.SerializeCustomDebugInformationTable(writer, metadataSizes);
            }

            writer.WriteByte(0);
            writer.Align(4);

            int endPosition = writer.Count;
            Debug.Assert(metadataSizes.MetadataTableStreamSize == endPosition - startPosition);
        }

        private void SerializeTablesHeader(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            int startPosition = writer.Count;

            HeapSizeFlag heapSizes = 0;
            if (!metadataSizes.StringReferenceIsSmall)
            {
                heapSizes |= HeapSizeFlag.StringHeapLarge;
            }

            if (!metadataSizes.GuidReferenceIsSmall)
            {
                heapSizes |= HeapSizeFlag.GuidHeapLarge;
            }

            if (!metadataSizes.BlobReferenceIsSmall)
            {
                heapSizes |= HeapSizeFlag.BlobHeapLarge;
            }

            if (metadataSizes.IsMinimalDelta)
            {
                heapSizes |= (HeapSizeFlag.EnCDeltas | HeapSizeFlag.DeletedMarks);
            }

            ulong sortedDebugTables = metadataSizes.PresentTablesMask & MetadataSizes.SortedDebugTables;

            // Consider filtering out type system tables that are not present:
            ulong sortedTables = sortedDebugTables | (metadataSizes.IsStandaloneDebugMetadata ? 0UL : 0x16003301fa00);

            writer.WriteUInt32(0); // reserved
            writer.WriteByte(MetadataFormatMajorVersion);
            writer.WriteByte(MetadataFormatMinorVersion);
            writer.WriteByte((byte)heapSizes);
            writer.WriteByte(1); // reserved
            writer.WriteUInt64(metadataSizes.PresentTablesMask);
            writer.WriteUInt64(sortedTables);
            MetadataWriterUtilities.SerializeRowCounts(writer, metadataSizes.RowCounts);

            int endPosition = writer.Count;
            Debug.Assert(metadataSizes.CalculateTableStreamHeaderSize() == endPosition - startPosition);
        }

        // internal for testing
        internal void SerializeModuleTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (var moduleRow in _moduleTable)
            {
                writer.WriteUInt16(moduleRow.Generation);
                writer.WriteReference(SerializeHandle(moduleRow.Name), metadataSizes.StringReferenceIsSmall);
                writer.WriteReference(SerializeHandle(moduleRow.ModuleVersionId), metadataSizes.GuidReferenceIsSmall);
                writer.WriteReference(SerializeHandle(moduleRow.EncId), metadataSizes.GuidReferenceIsSmall);
                writer.WriteReference(SerializeHandle(moduleRow.EncBaseId), metadataSizes.GuidReferenceIsSmall);
            }
        }

        private void SerializeEncLogTable(BlobBuilder writer)
        {
            foreach (EncLogRow encLog in _encLogTable)
            {
                writer.WriteInt32(encLog.Token);
                writer.WriteUInt32(encLog.FuncCode);
            }
        }

        private void SerializeEncMapTable(BlobBuilder writer)
        {
            foreach (EncMapRow encMap in _encMapTable)
            {
                writer.WriteInt32(encMap.Token);
            }
        }

        private void SerializeTypeRefTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (TypeRefRow typeRef in _typeRefTable)
            {
                writer.WriteReference(typeRef.ResolutionScope, metadataSizes.ResolutionScopeCodedIndexIsSmall);
                writer.WriteReference(SerializeHandle(typeRef.Name), metadataSizes.StringReferenceIsSmall);
                writer.WriteReference(SerializeHandle(typeRef.Namespace), metadataSizes.StringReferenceIsSmall);
            }
        }

        private void SerializeTypeDefTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (TypeDefRow typeDef in _typeDefTable)
            {
                writer.WriteUInt32(typeDef.Flags);
                writer.WriteReference(SerializeHandle(typeDef.Name), metadataSizes.StringReferenceIsSmall);
                writer.WriteReference(SerializeHandle(typeDef.Namespace), metadataSizes.StringReferenceIsSmall);
                writer.WriteReference(typeDef.Extends, metadataSizes.TypeDefOrRefCodedIndexIsSmall);
                writer.WriteReference(typeDef.FieldList, metadataSizes.FieldDefReferenceIsSmall);
                writer.WriteReference(typeDef.MethodList, metadataSizes.MethodDefReferenceIsSmall);
            }
        }

        private void SerializeFieldTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (FieldDefRow fieldDef in _fieldTable)
            {
                writer.WriteUInt16(fieldDef.Flags);
                writer.WriteReference(SerializeHandle(fieldDef.Name), metadataSizes.StringReferenceIsSmall);
                writer.WriteReference(SerializeHandle(fieldDef.Signature), metadataSizes.BlobReferenceIsSmall);
            }
        }

        private void SerializeMethodDefTable(BlobBuilder writer, MetadataSizes metadataSizes, int methodBodyStreamRva)
        {
            foreach (MethodRow method in _methodDefTable)
            {
                if (method.BodyOffset == -1)
                {
                    writer.WriteUInt32(0);
                }
                else
                {
                    writer.WriteInt32(methodBodyStreamRva + method.BodyOffset);
                }

                writer.WriteUInt16(method.ImplFlags);
                writer.WriteUInt16(method.Flags);
                writer.WriteReference(SerializeHandle(method.Name), metadataSizes.StringReferenceIsSmall);
                writer.WriteReference(SerializeHandle(method.Signature), metadataSizes.BlobReferenceIsSmall);
                writer.WriteReference(method.ParamList, metadataSizes.ParameterReferenceIsSmall);
            }
        }

        private void SerializeParamTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (ParamRow param in _paramTable)
            {
                writer.WriteUInt16(param.Flags);
                writer.WriteUInt16(param.Sequence);
                writer.WriteReference(SerializeHandle(param.Name), metadataSizes.StringReferenceIsSmall);
            }
        }

        private void SerializeInterfaceImplTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            // TODO (bug https://github.com/dotnet/roslyn/issues/3905):
            // We should sort the table by Class and then by Interface.
            foreach (InterfaceImplRow interfaceImpl in _interfaceImplTable)
            {
                writer.WriteReference(interfaceImpl.Class, metadataSizes.TypeDefReferenceIsSmall);
                writer.WriteReference(interfaceImpl.Interface, metadataSizes.TypeDefOrRefCodedIndexIsSmall);
            }
        }

        private void SerializeMemberRefTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (MemberRefRow memberRef in _memberRefTable)
            {
                writer.WriteReference(memberRef.Class, metadataSizes.MemberRefParentCodedIndexIsSmall);
                writer.WriteReference(SerializeHandle(memberRef.Name), metadataSizes.StringReferenceIsSmall);
                writer.WriteReference(SerializeHandle(memberRef.Signature), metadataSizes.BlobReferenceIsSmall);
            }
        }

        private void SerializeConstantTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            // Note: we can sort the table at this point since no other table can reference its rows via RowId or CodedIndex (which would need updating otherwise).
            var ordered = _constantTableNeedsSorting ? (IEnumerable<ConstantRow>)_constantTable.OrderBy((x, y) => (int)x.Parent - (int)y.Parent) : _constantTable;

            foreach (ConstantRow constant in ordered)
            {
                writer.WriteByte(constant.Type);
                writer.WriteByte(0);
                writer.WriteReference(constant.Parent, metadataSizes.HasConstantCodedIndexIsSmall);
                writer.WriteReference(SerializeHandle(constant.Value), metadataSizes.BlobReferenceIsSmall);
            }
        }

        private void SerializeCustomAttributeTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            // Note: we can sort the table at this point since no other table can reference its rows via RowId or CodedIndex (which would need updating otherwise).
            // OrderBy performs a stable sort, so multiple attributes with the same parent will be sorted in the order they were added to the table.
            var ordered = _customAttributeTableNeedsSorting ? (IEnumerable<CustomAttributeRow>)_customAttributeTable.OrderBy((x, y) => (int)x.Parent - (int)y.Parent) : _customAttributeTable;

            foreach (CustomAttributeRow customAttribute in ordered)
            {
                writer.WriteReference(customAttribute.Parent, metadataSizes.HasCustomAttributeCodedIndexIsSmall);
                writer.WriteReference(customAttribute.Type, metadataSizes.CustomAttributeTypeCodedIndexIsSmall);
                writer.WriteReference(SerializeHandle(customAttribute.Value), metadataSizes.BlobReferenceIsSmall);
            }
        }

        private void SerializeFieldMarshalTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            // Note: we can sort the table at this point since no other table can reference its rows via RowId or CodedIndex (which would need updating otherwise).
            var ordered = _fieldMarshalTableNeedsSorting ? (IEnumerable<FieldMarshalRow>)_fieldMarshalTable.OrderBy((x, y) => (int)x.Parent - (int)y.Parent) : _fieldMarshalTable;
            
            foreach (FieldMarshalRow fieldMarshal in ordered)
            {
                writer.WriteReference(fieldMarshal.Parent, metadataSizes.HasFieldMarshalCodedIndexIsSmall);
                writer.WriteReference(SerializeHandle(fieldMarshal.NativeType), metadataSizes.BlobReferenceIsSmall);
            }
        }

        private void SerializeDeclSecurityTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            // Note: we can sort the table at this point since no other table can reference its rows via RowId or CodedIndex (which would need updating otherwise).
            // OrderBy performs a stable sort, so multiple attributes with the same parent will be sorted in the order they were added to the table.
            var ordered = _declSecurityTableNeedsSorting ? (IEnumerable<DeclSecurityRow>)_declSecurityTable.OrderBy((x, y) => (int)x.Parent - (int)y.Parent) : _declSecurityTable;
            
            foreach (DeclSecurityRow declSecurity in ordered)
            {
                writer.WriteUInt16(declSecurity.Action);
                writer.WriteReference(declSecurity.Parent, metadataSizes.DeclSecurityCodedIndexIsSmall);
                writer.WriteReference(SerializeHandle(declSecurity.PermissionSet), metadataSizes.BlobReferenceIsSmall);
            }
        }

        private void SerializeClassLayoutTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
#if DEBUG
            for (int i = 1; i < _classLayoutTable.Count; i++)
            {
                Debug.Assert(_classLayoutTable[i - 1].Parent < _classLayoutTable[i].Parent);
            }
#endif
            foreach (ClassLayoutRow classLayout in _classLayoutTable)
            {
                writer.WriteUInt16(classLayout.PackingSize);
                writer.WriteUInt32(classLayout.ClassSize);
                writer.WriteReference(classLayout.Parent, metadataSizes.TypeDefReferenceIsSmall);
            }
        }

        private void SerializeFieldLayoutTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
#if DEBUG
            for (int i = 1; i < _fieldLayoutTable.Count; i++)
            {
                Debug.Assert(_fieldLayoutTable[i - 1].Field < _fieldLayoutTable[i].Field);
            }
#endif
            foreach (FieldLayoutRow fieldLayout in _fieldLayoutTable)
            {
                writer.WriteInt32(fieldLayout.Offset);
                writer.WriteReference(fieldLayout.Field, metadataSizes.FieldDefReferenceIsSmall);
            }
        }

        private void SerializeStandAloneSigTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (StandaloneSigRow row in _standAloneSigTable)
            {
                writer.WriteReference(SerializeHandle(row.Signature), metadataSizes.BlobReferenceIsSmall);
            }
        }

        private void SerializeEventMapTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (EventMapRow eventMap in _eventMapTable)
            {
                writer.WriteReference(eventMap.Parent, metadataSizes.TypeDefReferenceIsSmall);
                writer.WriteReference(eventMap.EventList, metadataSizes.EventDefReferenceIsSmall);
            }
        }

        private void SerializeEventTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (EventRow eventRow in _eventTable)
            {
                writer.WriteUInt16(eventRow.EventFlags);
                writer.WriteReference(SerializeHandle(eventRow.Name), metadataSizes.StringReferenceIsSmall);
                writer.WriteReference(eventRow.EventType, metadataSizes.TypeDefOrRefCodedIndexIsSmall);
            }
        }

        private void SerializePropertyMapTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (PropertyMapRow propertyMap in _propertyMapTable)
            {
                writer.WriteReference(propertyMap.Parent, metadataSizes.TypeDefReferenceIsSmall);
                writer.WriteReference(propertyMap.PropertyList, metadataSizes.PropertyDefReferenceIsSmall);
            }
        }

        private void SerializePropertyTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (PropertyRow property in _propertyTable)
            {
                writer.WriteUInt16(property.PropFlags);
                writer.WriteReference(SerializeHandle(property.Name), metadataSizes.StringReferenceIsSmall);
                writer.WriteReference(SerializeHandle(property.Type), metadataSizes.BlobReferenceIsSmall);
            }
        }

        private void SerializeMethodSemanticsTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            // Note: we can sort the table at this point since no other table can reference its rows via RowId or CodedIndex (which would need updating otherwise).
            // OrderBy performs a stable sort, so multiple attributes with the same parent will be sorted in the order they were added to the table.
            var ordered = _methodSemanticsTableNeedsSorting ? (IEnumerable<MethodSemanticsRow>)_methodSemanticsTable.OrderBy((x, y) => (int)x.Association - (int)y.Association) : _methodSemanticsTable;
            
            foreach (MethodSemanticsRow methodSemantic in ordered)
            {
                writer.WriteUInt16(methodSemantic.Semantic);
                writer.WriteReference(methodSemantic.Method, metadataSizes.MethodDefReferenceIsSmall);
                writer.WriteReference(methodSemantic.Association, metadataSizes.HasSemanticsCodedIndexIsSmall);
            }
        }

        private void SerializeMethodImplTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
#if DEBUG
            for (int i = 1; i < _methodImplTable.Count; i++)
            {
                Debug.Assert(_methodImplTable[i - 1].Class <= _methodImplTable[i].Class);
            }
#endif
            foreach (MethodImplRow methodImpl in _methodImplTable)
            {
                writer.WriteReference(methodImpl.Class, metadataSizes.TypeDefReferenceIsSmall);
                writer.WriteReference(methodImpl.MethodBody, metadataSizes.MethodDefOrRefCodedIndexIsSmall);
                writer.WriteReference(methodImpl.MethodDecl, metadataSizes.MethodDefOrRefCodedIndexIsSmall);
            }
        }

        private void SerializeModuleRefTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (ModuleRefRow moduleRef in _moduleRefTable)
            {
                writer.WriteReference(SerializeHandle(moduleRef.Name), metadataSizes.StringReferenceIsSmall);
            }
        }

        private void SerializeTypeSpecTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (TypeSpecRow typeSpec in _typeSpecTable)
            {
                writer.WriteReference(SerializeHandle(typeSpec.Signature), metadataSizes.BlobReferenceIsSmall);
            }
        }

        private void SerializeImplMapTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
#if DEBUG
            for (int i = 1; i < _implMapTable.Count; i++)
            {
                Debug.Assert(_implMapTable[i - 1].MemberForwarded < _implMapTable[i].MemberForwarded);
            }
#endif
            foreach (ImplMapRow implMap in _implMapTable)
            {
                writer.WriteUInt16(implMap.MappingFlags);
                writer.WriteReference(implMap.MemberForwarded, metadataSizes.MemberForwardedCodedIndexIsSmall);
                writer.WriteReference(SerializeHandle(implMap.ImportName), metadataSizes.StringReferenceIsSmall);
                writer.WriteReference(implMap.ImportScope, metadataSizes.ModuleRefReferenceIsSmall);
            }
        }

        private void SerializeFieldRvaTable(BlobBuilder writer, MetadataSizes metadataSizes, int mappedFieldDataStreamRva)
        {
#if DEBUG
            for (int i = 1; i < _fieldRvaTable.Count; i++)
            {
                Debug.Assert(_fieldRvaTable[i - 1].Field < _fieldRvaTable[i].Field);
            }
#endif
            foreach (FieldRvaRow fieldRva in _fieldRvaTable)
            {
                writer.WriteUInt32((uint)mappedFieldDataStreamRva + fieldRva.Offset);
                writer.WriteReference(fieldRva.Field, metadataSizes.FieldDefReferenceIsSmall);
            }
        }

        private void SerializeAssemblyTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (AssemblyRow row in _assemblyTable)
            {
                writer.WriteUInt32(row.HashAlgorithm);
                writer.WriteUInt16((ushort)row.Version.Major);
                writer.WriteUInt16((ushort)row.Version.Minor);
                writer.WriteUInt16((ushort)row.Version.Build);
                writer.WriteUInt16((ushort)row.Version.Revision);
                writer.WriteUInt32(row.Flags);
                writer.WriteReference(SerializeHandle(row.AssemblyKey), metadataSizes.BlobReferenceIsSmall);
                writer.WriteReference(SerializeHandle(row.AssemblyName), metadataSizes.StringReferenceIsSmall);
                writer.WriteReference(SerializeHandle(row.AssemblyCulture), metadataSizes.StringReferenceIsSmall);
            }
        }

        private void SerializeAssemblyRefTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (AssemblyRefTableRow row in _assemblyRefTable)
            {
                writer.WriteUInt16((ushort)row.Version.Major);
                writer.WriteUInt16((ushort)row.Version.Minor);
                writer.WriteUInt16((ushort)row.Version.Build);
                writer.WriteUInt16((ushort)row.Version.Revision);
                writer.WriteUInt32(row.Flags);
                writer.WriteReference(SerializeHandle(row.PublicKeyToken), metadataSizes.BlobReferenceIsSmall);
                writer.WriteReference(SerializeHandle(row.Name), metadataSizes.StringReferenceIsSmall);
                writer.WriteReference(SerializeHandle(row.Culture), metadataSizes.StringReferenceIsSmall);
                writer.WriteReference(SerializeHandle(row.HashValue), metadataSizes.BlobReferenceIsSmall);
            }
        }

        private void SerializeFileTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (FileTableRow fileReference in _fileTable)
            {
                writer.WriteUInt32(fileReference.Flags);
                writer.WriteReference(SerializeHandle(fileReference.FileName), metadataSizes.StringReferenceIsSmall);
                writer.WriteReference(SerializeHandle(fileReference.HashValue), metadataSizes.BlobReferenceIsSmall);
            }
        }

        private void SerializeExportedTypeTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (ExportedTypeRow exportedType in _exportedTypeTable)
            {
                writer.WriteUInt32(exportedType.Flags);
                writer.WriteInt32(exportedType.TypeDefId);
                writer.WriteReference(SerializeHandle(exportedType.TypeName), metadataSizes.StringReferenceIsSmall);
                writer.WriteReference(SerializeHandle(exportedType.TypeNamespace), metadataSizes.StringReferenceIsSmall);
                writer.WriteReference(exportedType.Implementation, metadataSizes.ImplementationCodedIndexIsSmall);
            }
        }

        private void SerializeManifestResourceTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (ManifestResourceRow manifestResource in _manifestResourceTable)
            {
                writer.WriteUInt32(manifestResource.Offset);
                writer.WriteUInt32(manifestResource.Flags);
                writer.WriteReference(SerializeHandle(manifestResource.Name), metadataSizes.StringReferenceIsSmall);
                writer.WriteReference(manifestResource.Implementation, metadataSizes.ImplementationCodedIndexIsSmall);
            }
        }

        private void SerializeNestedClassTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
#if DEBUG
            for (int i = 1; i < _nestedClassTable.Count; i++)
            {
                Debug.Assert(_nestedClassTable[i - 1].NestedClass <= _nestedClassTable[i].NestedClass);
            }
#endif
            foreach (NestedClassRow nestedClass in _nestedClassTable)
            {
                writer.WriteReference(nestedClass.NestedClass, metadataSizes.TypeDefReferenceIsSmall);
                writer.WriteReference(nestedClass.EnclosingClass, metadataSizes.TypeDefReferenceIsSmall);
            }
        }

        private void SerializeGenericParamTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
#if DEBUG
            for (int i = 1; i < _genericParamTable.Count; i++)
            {
                Debug.Assert(
                    _genericParamTable[i - 1].Owner < _genericParamTable[i].Owner ||
                    _genericParamTable[i - 1].Owner == _genericParamTable[i].Owner && _genericParamTable[i - 1].Number < _genericParamTable[i].Number);
            }
#endif            
            foreach (GenericParamRow genericParam in _genericParamTable)
            {
                writer.WriteUInt16(genericParam.Number);
                writer.WriteUInt16(genericParam.Flags);
                writer.WriteReference(genericParam.Owner, metadataSizes.TypeOrMethodDefCodedIndexIsSmall);
                writer.WriteReference(SerializeHandle(genericParam.Name), metadataSizes.StringReferenceIsSmall);
            }
        }

        private void SerializeGenericParamConstraintTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
#if DEBUG
            for (int i = 1; i < _genericParamConstraintTable.Count; i++)
            {
                Debug.Assert(_genericParamConstraintTable[i - 1].Owner <= _genericParamConstraintTable[i].Owner);
            }
#endif
            foreach (GenericParamConstraintRow genericParamConstraint in _genericParamConstraintTable)
            {
                writer.WriteReference(genericParamConstraint.Owner, metadataSizes.GenericParamReferenceIsSmall);
                writer.WriteReference(genericParamConstraint.Constraint, metadataSizes.TypeDefOrRefCodedIndexIsSmall);
            }
        }

        private void SerializeMethodSpecTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (MethodSpecRow methodSpec in _methodSpecTable)
            {
                writer.WriteReference(methodSpec.Method, metadataSizes.MethodDefOrRefCodedIndexIsSmall);
                writer.WriteReference(SerializeHandle(methodSpec.Instantiation), metadataSizes.BlobReferenceIsSmall);
            }
        }

        private void SerializeDocumentTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (var row in _documentTable)
            {
                writer.WriteReference(SerializeHandle(row.Name), metadataSizes.BlobReferenceIsSmall);
                writer.WriteReference(SerializeHandle(row.HashAlgorithm), metadataSizes.GuidReferenceIsSmall);
                writer.WriteReference(SerializeHandle(row.Hash), metadataSizes.BlobReferenceIsSmall);
                writer.WriteReference(SerializeHandle(row.Language), metadataSizes.GuidReferenceIsSmall);
            }
        }

        private void SerializeMethodDebugInformationTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (var row in _methodDebugInformationTable)
            {
                writer.WriteReference(row.Document, metadataSizes.DocumentReferenceIsSmall);
                writer.WriteReference(SerializeHandle(row.SequencePoints), metadataSizes.BlobReferenceIsSmall);
            }
        }

        private void SerializeLocalScopeTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
#if DEBUG
            // Spec: The table is required to be sorted first by Method in ascending order, then by StartOffset in ascending order, then by Length in descending order.
            for (int i = 1; i < _localScopeTable.Count; i++)
            {
                Debug.Assert(_localScopeTable[i - 1].Method <= _localScopeTable[i].Method);
                if (_localScopeTable[i - 1].Method == _localScopeTable[i].Method)
                {
                    Debug.Assert(_localScopeTable[i - 1].StartOffset <= _localScopeTable[i].StartOffset);
                    if (_localScopeTable[i - 1].StartOffset == _localScopeTable[i].StartOffset)
                    {
                        Debug.Assert(_localScopeTable[i - 1].Length >= _localScopeTable[i].Length);
                    }
                }
            }
#endif
            foreach (var row in _localScopeTable)
            {
                writer.WriteReference(row.Method, metadataSizes.MethodDefReferenceIsSmall);
                writer.WriteReference(row.ImportScope, metadataSizes.ImportScopeReferenceIsSmall);
                writer.WriteReference(row.VariableList, metadataSizes.LocalVariableReferenceIsSmall);
                writer.WriteReference(row.ConstantList, metadataSizes.LocalConstantReferenceIsSmall);
                writer.WriteInt32(row.StartOffset);
                writer.WriteInt32(row.Length);
            }
        }

        private void SerializeLocalVariableTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (var row in _localVariableTable)
            {
                writer.WriteUInt16(row.Attributes);
                writer.WriteUInt16(row.Index);
                writer.WriteReference(SerializeHandle(row.Name), metadataSizes.StringReferenceIsSmall);
            }
        }

        private void SerializeLocalConstantTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (var row in _localConstantTable)
            {
                writer.WriteReference(SerializeHandle(row.Name), metadataSizes.StringReferenceIsSmall);
                writer.WriteReference(SerializeHandle(row.Signature), metadataSizes.BlobReferenceIsSmall);
            }
        }

        private void SerializeImportScopeTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            foreach (var row in _importScopeTable)
            {
                writer.WriteReference(row.Parent, metadataSizes.ImportScopeReferenceIsSmall);
                writer.WriteReference(SerializeHandle(row.Imports), metadataSizes.BlobReferenceIsSmall);
            }
        }

        private void SerializeStateMachineMethodTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
#if DEBUG
            for (int i = 1; i < _stateMachineMethodTable.Count; i++)
            {
                Debug.Assert(_stateMachineMethodTable[i - 1].MoveNextMethod < _stateMachineMethodTable[i].MoveNextMethod);
            }
#endif
            foreach (var row in _stateMachineMethodTable)
            {
                writer.WriteReference(row.MoveNextMethod, metadataSizes.MethodDefReferenceIsSmall);
                writer.WriteReference(row.KickoffMethod, metadataSizes.MethodDefReferenceIsSmall);
            }
        }

        private void SerializeCustomDebugInformationTable(BlobBuilder writer, MetadataSizes metadataSizes)
        {
            // Note: we can sort the table at this point since no other table can reference its rows via RowId or CodedIndex (which would need updating otherwise).
            // OrderBy performs a stable sort, so multiple attributes with the same parent and kind will be sorted in the order they were added to the table.
            foreach (CustomDebugInformationRow row in _customDebugInformationTable.OrderBy((x, y) =>
            {
                int result = x.Parent - y.Parent;
                return (result != 0) ? result : x.Kind.Index - y.Kind.Index;
            }))
            {
                writer.WriteReference(row.Parent, metadataSizes.HasCustomDebugInformationCodedIndexIsSmall);
                writer.WriteReference(SerializeHandle(row.Kind), metadataSizes.GuidReferenceIsSmall);
                writer.WriteReference(SerializeHandle(row.Value), metadataSizes.BlobReferenceIsSmall);
            }
        }

        #endregion
    }
}
