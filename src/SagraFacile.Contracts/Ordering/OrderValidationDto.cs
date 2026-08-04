namespace SagraFacile.Contracts.Ordering;

public record ValidateOrderCompositionRequestLine(int MenuItemId, int Quantity);

public record ValidateOrderCompositionRequest(
    int Covers,
    List<ValidateOrderCompositionRequestLine> Lines);
