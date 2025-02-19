#include "Memory.h"
#include "Globals.h"

#define EXPORT __declspec(dllexport)

extern "C"
{
	EXPORT void Init()
	{
		Memory::Init();
	}

	EXPORT void* Allocate(int64 size)
	{
		return Memory::ms_pAllocator->Allocate(size);
	}

	EXPORT void Free(void* ptr)
	{
		Memory::ms_pAllocator->Free(ptr);
	}

	EXPORT bool DoesTextureDictionaryExist(const char* name)
	{
		return Memory::DoesTextureDictionaryExist(name);
	}

	EXPORT uint32 GetNumberOfTexturesFromDictionary(const char* name)
	{
		return Memory::GetNumberOfTexturesFromDictionary(name);
	}

	EXPORT void GetTexturesFromDictionary(const char* name, Memory::TextureDesc* outTextureDescs)
	{
		Memory::GetTexturesFromDictionary(name, outTextureDescs);
	}

	EXPORT bool DoesCustomTextureExist(uint32 nameHash)
	{
		return Memory::DoesCustomTextureExist(nameHash);
	}

	EXPORT bool CreateCustomTexture(const char* name, uint32 width, uint32 height, uint8* pixelData, bool updatable)
	{
		return Memory::CreateCustomTexture(name, width, height, pixelData, updatable);
	}

	EXPORT void DeleteCustomTexture(uint32 nameHash)
	{
		Memory::DeleteCustomTexture(nameHash);
	}

	EXPORT void UpdateCustomTexture(uint32 nameHash, const uint8* srcData, const RECT* dstRect)
	{
		Memory::UpdateCustomTexture(nameHash, srcData, *dstRect);
	}

	EXPORT uint32 GetNumberOfCustomTextures()
	{
		return Memory::GetNumberOfCustomTextures();
	}

	EXPORT void GetCustomTextures(Memory::CustomTextureDesc* outTextureDescs)
	{
		Memory::GetCustomTextures(outTextureDescs);
	}

	EXPORT int Globals_GetMenusOpened()
	{
		return Globals::MenusOpened;
	}

	EXPORT void Globals_SetMenusOpened(int value)
	{
		Globals::MenusOpened = value;
	}
}

#undef EXPORT
