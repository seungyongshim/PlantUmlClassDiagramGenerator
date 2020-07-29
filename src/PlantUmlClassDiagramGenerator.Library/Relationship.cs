namespace PlantUmlClassDiagramGenerator.Library
{
    public class Relationship
    {
        protected TypeNameText _baseTypeName;
        protected TypeNameText _subTypeName;
        protected string _baseLabel;
        protected string _subLabel;
        private readonly string _symbol;

        public Relationship(TypeNameText baseTypeName, TypeNameText subTypeName, string symbol, string baseLabel = "", string subLabel = "")
        {
            _baseTypeName = baseTypeName;
            _subTypeName = subTypeName;
            _symbol = symbol;
            _baseLabel = string.IsNullOrWhiteSpace(baseLabel) ? "" : $" \"{baseLabel}\"";
            _subLabel = string.IsNullOrWhiteSpace(subLabel) ? "" : $" \"{subLabel}\"";
        }

        public override string ToString()
        {
            return $"{_baseTypeName.Identifier}{_baseLabel} {_symbol} {_subTypeName.Identifier}";
        }

        public bool Equals(Relationship other)
        {
            if (other == null) return false;
            return (this.GetHashCode().Equals(other.GetHashCode()));
        }

        public override bool Equals(object other)
        {
            Relationship mod = other as Relationship;
            if (mod != null)
                return Equals(mod);
            return false;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}