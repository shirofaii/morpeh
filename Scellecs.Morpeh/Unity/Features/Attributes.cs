using System;

public class CmpAttribute : Attribute { }
public class UCmpAttribute : Attribute { }
public class EvtAttribute : Attribute { }

public class FeatureAttribute : Attribute { }

public abstract class SystemAttribute : Attribute { }

public class PreAttribute : SystemAttribute { }
public class UpdAttribute : SystemAttribute { }
public class LatAttribute : SystemAttribute { }
public class FixAttribute : SystemAttribute { }
public class DrwAttribute : SystemAttribute { }
public class ClnAttribute : SystemAttribute { }
